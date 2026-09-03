using Lyricsongs.Application.Jobs;
using Lyricsongs.Application.Projects;
using Lyricsongs.Application.Subtitles;
using Lyricsongs.Application.VideoPlan;
using Lyricsongs.Domain.Jobs;
using Lyricsongs.Domain.Subtitles;
using Lyricsongs.Domain.Timeline;
using Lyricsongs.Domain.VideoPlan;

namespace Lyricsongs.Infrastructure.Projects;

public sealed class ProjectPlanService : IProjectPlanService
{
    private readonly IProjectStore _projects;
    private readonly IProjectFileStorage _storage;
    private readonly IProjectRenderValidator _validator;
    private readonly ISrtProcessingService _srtProcessing;
    private readonly IVideoPlanEngine _videoPlanEngine;

    public ProjectPlanService(
        IProjectStore projects,
        IProjectFileStorage storage,
        IProjectRenderValidator validator,
        ISrtProcessingService srtProcessing,
        IVideoPlanEngine videoPlanEngine)
    {
        _projects = projects;
        _storage = storage;
        _validator = validator;
        _srtProcessing = srtProcessing;
        _videoPlanEngine = videoPlanEngine;
    }

    public async Task<ProjectPlanResponse> BuildAsync(
        string projectId,
        VideoPlanSettings? settingsOverride = null,
        CancellationToken cancellationToken = default)
    {
        var project = await _projects.GetAsync(projectId, cancellationToken)
            ?? throw new InvalidOperationException($"Project '{projectId}' was not found.");

        var validation = _validator.Validate(project, _storage);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException(string.Join(' ', validation.Errors));
        }

        if (settingsOverride is not null)
        {
            project.SettingsJson = System.Text.Json.JsonSerializer.Serialize(settingsOverride);
            await _projects.SaveAsync(project, cancellationToken);
        }

        var lyrics = await LoadLyricsAsync(project, cancellationToken);

        var settings = DeserializeSettings(project.SettingsJson);
        var assets = project.Assets
            .OrderBy(a => a.SortOrder)
            .Select(a => new VideoPlanAsset
            {
                AssetId = a.Id,
                FileName = a.FileName,
                Type = a.Type.Equals("video", StringComparison.OrdinalIgnoreCase)
                    ? BackgroundAssetType.Video
                    : BackgroundAssetType.Image,
                SourceDurationMs = a.SourceDurationMs,
                SortOrder = a.SortOrder
            })
            .ToList();

        var buildResult = _videoPlanEngine.Build(new VideoPlanRequest
        {
            AudioDurationMs = project.AudioDurationMs ?? EstimateAudioDuration(lyrics),
            Lyrics = lyrics,
            Assets = assets,
            Settings = settings
        });

        var plan = buildResult.Plan;
        var assetNames = project.Assets.ToDictionary(a => a.Id, a => a.FileName, StringComparer.Ordinal);

        var timeline = plan.Backgrounds.Select(b => new ProjectPlanTimelineSegmentDto
        {
            AssetId = b.AssetId,
            FileName = assetNames.GetValueOrDefault(b.AssetId) ?? b.AssetId,
            Type = b.Type == BackgroundAssetType.Video ? "video" : "image",
            StartMs = b.StartMs,
            EndMs = b.EndMs,
            Animation = b.Animation.ToString(),
            TransitionOut = b.TransitionOut.ToString(),
            TransitionOutDurationMs = b.TransitionOutDurationMs
        }).ToList();

        var messages = buildResult.Validation.Issues
            .Select(i => i.Message)
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .ToList();

        return new ProjectPlanResponse
        {
            ProjectId = projectId,
            PlanId = plan.PlanId,
            AudioDurationMs = plan.AudioDurationMs,
            IsValid = buildResult.Validation.IsValid,
            Timeline = timeline,
            Lyrics = plan.Subtitles,
            Output = new ProjectPlanOutputDto
            {
                Width = plan.Output.Width,
                Height = plan.Output.Height,
                Fps = plan.Output.Fps,
                VideoCodec = plan.Output.VideoCodec,
                AudioCodec = plan.Output.AudioCodec,
                Format = plan.Output.Format,
                AspectRatio = plan.Output.AspectRatio.ToString()
            },
            LyricsStyle = new ProjectPlanLyricsDto
            {
                Style = plan.Lyrics.Style.ToString(),
                Font = plan.Lyrics.Font,
                FontSize = plan.Lyrics.FontSize,
                Position = plan.Lyrics.Position.ToString(),
                TextColor = plan.Lyrics.TextColor,
                HighlightColor = plan.Lyrics.HighlightColor
            },
            ValidationMessages = messages
        };
    }

    private async Task<IReadOnlyList<LyricCue>> LoadLyricsAsync(
        ProjectRecord project,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(project.SrtRelativePath))
        {
            return [];
        }

        var srtPath = _storage.GetAbsolutePath(project.SrtRelativePath);
        if (!File.Exists(srtPath))
        {
            return [];
        }

        await using var srtStream = File.OpenRead(srtPath);
        var srtResult = _srtProcessing.Process(srtStream);
        return srtResult.Lyrics;
    }

    private static VideoPlanSettings DeserializeSettings(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new VideoPlanSettings();
        }

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<VideoPlanSettings>(json)
                   ?? new VideoPlanSettings();
        }
        catch
        {
            return new VideoPlanSettings();
        }
    }

    private static long EstimateAudioDuration(IReadOnlyList<LyricCue> lyrics) =>
        lyrics.Count == 0 ? 60_000 : Math.Max(lyrics.Max(l => l.EndMs) + 2_000, 10_000);
}
