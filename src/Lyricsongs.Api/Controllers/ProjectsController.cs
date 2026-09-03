using System.Text;
using Lyricsongs.Application.Jobs;
using Lyricsongs.Application.Projects;
using Lyricsongs.Application.Rendering;
using Lyricsongs.Application.Subtitles;
using Lyricsongs.Domain.Jobs;
using Lyricsongs.Domain.Subtitles;
using Lyricsongs.Domain.VideoPlan;
using Lyricsongs.Infrastructure.Subtitles;
using Microsoft.AspNetCore.Mvc;

namespace Lyricsongs.Api.Controllers;

[ApiController]
[Route("api/v1/projects")]
[Route("api/projects")]
public sealed class ProjectsController : ControllerBase
{
    private readonly IProjectStore _projects;
    private readonly IProjectFileStorage _storage;
    private readonly IRenderJobStore _renderJobStore;
    private readonly IRenderJobService _renderJobs;
    private readonly IProjectPlanService _planService;
    private readonly IMediaProbeService _mediaProbe;
    private readonly ISrtProcessingService _srtProcessing;
    private readonly ISrtValidationService _srtValidation;

    public ProjectsController(
        IProjectStore projects,
        IProjectFileStorage storage,
        IRenderJobStore renderJobStore,
        IRenderJobService renderJobs,
        IProjectPlanService planService,
        IMediaProbeService mediaProbe,
        ISrtProcessingService srtProcessing,
        ISrtValidationService srtValidation)
    {
        _projects = projects;
        _storage = storage;
        _renderJobStore = renderJobStore;
        _renderJobs = renderJobs;
        _planService = planService;
        _mediaProbe = mediaProbe;
        _srtProcessing = srtProcessing;
        _srtValidation = srtValidation;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProjectSummaryDto>>> ListAsync(CancellationToken cancellationToken)
    {
        var projects = await _projects.ListAsync(cancellationToken);
        return Ok(projects.Select(ToSummary).ToList());
    }

    [HttpPost]
    public async Task<ActionResult<ProjectSummaryDto>> CreateAsync(
        [FromBody] CreateProjectRequest? request,
        CancellationToken cancellationToken)
    {
        var project = await _projects.CreateAsync(request?.Name ?? "Untitled Project", cancellationToken);
        var summary = ToSummary(project);
        return Created($"/api/v1/projects/{project.Id}", summary);
    }

    [HttpGet("{projectId}")]
    public async Task<ActionResult<ProjectDetailDto>> GetAsync(string projectId, CancellationToken cancellationToken)
    {
        var project = await _projects.GetAsync(projectId, cancellationToken);
        return project is null ? NotFound() : Ok(ToDetail(project));
    }

    [HttpDelete("{projectId}")]
    public async Task<ActionResult> DeleteAsync(string projectId, CancellationToken cancellationToken)
    {
        var project = await _projects.GetAsync(projectId, cancellationToken);
        if (project is null)
        {
            return NotFound();
        }

        await _renderJobStore.DeleteByProjectIdAsync(projectId, cancellationToken);
        var deleted = await _projects.DeleteAsync(projectId, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    [HttpPost("{projectId}/audio")]
    [RequestSizeLimit(200 * 1024 * 1024)]
    public async Task<ActionResult<ProjectAssetDto>> UploadAudioAsync(
        string projectId,
        [FromForm] IFormFile file,
        [FromForm] long? durationMs,
        CancellationToken cancellationToken)
    {
        var project = await RequireProjectAsync(projectId, cancellationToken);
        if (file is null || file.Length == 0)
        {
            return BadRequest("Audio file is required.");
        }

        await using var stream = file.OpenReadStream();
        var relative = await _storage.SaveProjectFileAsync(
            projectId, "audio", file.FileName, stream, cancellationToken);

        var absolute = _storage.GetAbsolutePath(relative);
        var probed = durationMs ?? await _mediaProbe.ProbeDurationMsAsync(absolute, cancellationToken);

        project.AudioRelativePath = relative;
        project.AudioFileName = file.FileName;
        project.AudioDurationMs = probed;
        await _projects.SaveAsync(project, cancellationToken);

        return Ok(new ProjectAssetDto
        {
            Id = "audio",
            FileName = file.FileName,
            Type = "audio",
            RelativePath = relative,
            SourceDurationMs = probed
        });
    }

    [HttpGet("{projectId}/audio")]
    public async Task<IActionResult> DownloadAudioAsync(string projectId, CancellationToken cancellationToken)
    {
        var project = await RequireProjectAsync(projectId, cancellationToken);
        if (string.IsNullOrWhiteSpace(project.AudioRelativePath))
        {
            return NotFound("Project has no audio file.");
        }

        var absolute = _storage.GetAbsolutePath(project.AudioRelativePath);
        if (!System.IO.File.Exists(absolute))
        {
            return NotFound("Audio file is missing on disk.");
        }

        var contentType = GuessAudioContentType(project.AudioFileName ?? absolute);
        return PhysicalFile(absolute, contentType, enableRangeProcessing: true);
    }

    [HttpPost("{projectId}/srt")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<ActionResult<SrtUploadResponseDto>> UploadSrtAsync(
        string projectId,
        [FromForm] IFormFile file,
        CancellationToken cancellationToken)
    {
        var project = await RequireProjectAsync(projectId, cancellationToken);
        if (file is null || file.Length == 0)
        {
            return BadRequest("SRT file is required.");
        }

        await using var stream = file.OpenReadStream();
        var relative = await _storage.SaveProjectFileAsync(
            projectId, "srt", file.FileName, stream, cancellationToken);

        await using var parseStream = System.IO.File.OpenRead(_storage.GetAbsolutePath(relative));
        var parseResult = _srtProcessing.Process(parseStream);

        project.SrtRelativePath = relative;
        project.SrtFileName = file.FileName;
        project.LyricCount = parseResult.Lyrics.Count;
        await _projects.SaveAsync(project, cancellationToken);

        return Ok(new SrtUploadResponseDto
        {
            FileName = file.FileName,
            RelativePath = relative,
            IsValid = parseResult.IsValid,
            LyricCount = parseResult.Lyrics.Count,
            Lyrics = parseResult.Lyrics.Select(ToLyricDto).ToList(),
            Issues = parseResult.Issues.Select(i => i.Message).ToList()
        });
    }

    [HttpPut("{projectId}/srt")]
    public async Task<ActionResult<SrtUploadResponseDto>> UpdateSrtLyricsAsync(
        string projectId,
        [FromBody] UpdateLyricsRequest request,
        CancellationToken cancellationToken)
    {
        var project = await RequireProjectAsync(projectId, cancellationToken);

        if (string.IsNullOrWhiteSpace(project.SrtRelativePath))
        {
            project.SrtRelativePath = $"projects/{projectId}/srt/lyrics.srt";
            project.SrtFileName = "lyrics.srt";
        }

        var cues = (request.Lyrics ?? [])
            .Select((dto, order) => ToLyricCue(dto, order + 1))
            .ToList();

        if (cues.Count > 0)
        {
            var validation = _srtValidation.Validate(cues);
            var errors = validation.Issues
                .Where(i => i.Severity == SrtValidationSeverity.Error)
                .Select(i => i.Message)
                .ToList();
            if (errors.Count > 0)
            {
                return BadRequest(new { errors });
            }
        }

        var content = SrtWriter.Serialize(cues);
        var absolute = _storage.GetAbsolutePath(project.SrtRelativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(absolute)!);
        await System.IO.File.WriteAllTextAsync(absolute, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), cancellationToken);

        var parseResult = _srtProcessing.Process(content);
        project.LyricCount = parseResult.Lyrics.Count > 0 ? parseResult.Lyrics.Count : cues.Count;
        await _projects.SaveAsync(project, cancellationToken);

        var responseLyrics = parseResult.Lyrics.Count > 0 ? parseResult.Lyrics : cues;

        return Ok(new SrtUploadResponseDto
        {
            FileName = project.SrtFileName ?? "lyrics.srt",
            RelativePath = project.SrtRelativePath,
            IsValid = parseResult.IsValid,
            LyricCount = responseLyrics.Count,
            Lyrics = responseLyrics.Select(ToLyricDto).ToList(),
            Issues = parseResult.Issues.Select(i => i.Message).ToList()
        });
    }

    [HttpPost("{projectId}/assets")]
    [RequestSizeLimit(500 * 1024 * 1024)]
    public async Task<ActionResult<ProjectAssetDto>> UploadAssetAsync(
        string projectId,
        [FromForm] IFormFile file,
        [FromForm] string type,
        [FromForm] long? durationMs,
        [FromForm] int? sortOrder,
        [FromForm] string? assetId,
        CancellationToken cancellationToken)
    {
        var project = await RequireProjectAsync(projectId, cancellationToken);
        if (file is null || file.Length == 0)
        {
            return BadRequest("Asset file is required.");
        }

        var normalizedType = type?.Trim().ToLowerInvariant() switch
        {
            "video" => "video",
            _ => "image"
        };

        await using var stream = file.OpenReadStream();
        var folder = normalizedType == "video" ? "videos" : "images";
        var relative = await _storage.SaveProjectFileAsync(
            projectId, folder, file.FileName, stream, cancellationToken);

        var absolute = _storage.GetAbsolutePath(relative);
        long? resolvedDuration = durationMs;
        if (normalizedType == "video" && resolvedDuration is null)
        {
            resolvedDuration = await _mediaProbe.ProbeDurationMsAsync(absolute, cancellationToken);
        }

        var record = new ProjectAssetRecord
        {
            Id = string.IsNullOrWhiteSpace(assetId) ? Guid.NewGuid().ToString("N") : assetId.Trim(),
            FileName = file.FileName,
            RelativePath = relative,
            Type = normalizedType,
            SourceDurationMs = resolvedDuration,
            SortOrder = sortOrder ?? project.Assets.Count
        };

        project.Assets.RemoveAll(a => a.Id == record.Id);
        project.Assets.Add(record);
        await _projects.SaveAsync(project, cancellationToken);

        return Ok(new ProjectAssetDto
        {
            Id = record.Id,
            FileName = record.FileName,
            Type = record.Type,
            RelativePath = record.RelativePath,
            SourceDurationMs = record.SourceDurationMs,
            SortOrder = record.SortOrder
        });
    }

    [HttpPut("{projectId}/assets/order")]
    public async Task<ActionResult> ReorderAssetsAsync(
        string projectId,
        [FromBody] ReorderAssetsRequest request,
        CancellationToken cancellationToken)
    {
        var project = await RequireProjectAsync(projectId, cancellationToken);
        var orderMap = request.Assets
            .Select((a, index) => new { a.Id, SortOrder = a.SortOrder ?? index })
            .ToDictionary(x => x.Id, x => x.SortOrder, StringComparer.Ordinal);

        foreach (var asset in project.Assets)
        {
            if (orderMap.TryGetValue(asset.Id, out var sortOrder))
            {
                asset.SortOrder = sortOrder;
            }
        }

        project.Assets = project.Assets.OrderBy(a => a.SortOrder).ToList();
        await _projects.SaveAsync(project, cancellationToken);
        return NoContent();
    }

    [HttpPut("{projectId}/settings")]
    public async Task<ActionResult> UpdateSettingsAsync(
        string projectId,
        [FromBody] VideoPlanSettings settings,
        CancellationToken cancellationToken)
    {
        var project = await RequireProjectAsync(projectId, cancellationToken);
        project.SettingsJson = System.Text.Json.JsonSerializer.Serialize(settings);
        await _projects.SaveAsync(project, cancellationToken);
        return NoContent();
    }

    [HttpPost("{projectId}/plan")]
    public async Task<ActionResult<ProjectPlanResponse>> BuildPlanAsync(
        string projectId,
        [FromBody] VideoPlanSettings? settings,
        CancellationToken cancellationToken)
    {
        var plan = await _planService.BuildAsync(projectId, settings, cancellationToken);
        return Ok(plan);
    }

    [HttpPost("{projectId}/render")]
    public async Task<ActionResult<RenderJobDto>> StartRenderAsync(
        string projectId,
        [FromBody] CreateRenderJobRequest? request,
        CancellationToken cancellationToken)
    {
        var job = await _renderJobs.EnqueueAsync(
            projectId,
            request?.Settings,
            cancellationToken);
        return Accepted(job);
    }

    private async Task<ProjectRecord> RequireProjectAsync(string projectId, CancellationToken cancellationToken)
    {
        var project = await _projects.GetAsync(projectId, cancellationToken);
        if (project is null)
        {
            throw new FileNotFoundException($"Project '{projectId}' was not found.");
        }

        return project;
    }

    private static ProjectSummaryDto ToSummary(ProjectRecord project) => new()
    {
        Id = project.Id,
        Name = project.Name,
        CreatedAt = project.CreatedAt,
        UpdatedAt = project.UpdatedAt,
        Status = string.IsNullOrEmpty(project.AudioRelativePath) ? "draft" : "ready",
        AudioFileName = project.AudioFileName,
        DurationMs = project.AudioDurationMs,
        LyricCount = project.LyricCount,
        AssetCount = project.Assets.Count
    };

    private static ProjectDetailDto ToDetail(ProjectRecord project) => new()
    {
        Id = project.Id,
        Name = project.Name,
        CreatedAt = project.CreatedAt,
        UpdatedAt = project.UpdatedAt,
        AudioFileName = project.AudioFileName,
        AudioDurationMs = project.AudioDurationMs,
        SrtFileName = project.SrtFileName,
        LyricCount = project.LyricCount,
        Assets = project.Assets.Select(a => new ProjectAssetDto
        {
            Id = a.Id,
            FileName = a.FileName,
            Type = a.Type,
            RelativePath = a.RelativePath,
            SourceDurationMs = a.SourceDurationMs,
            SortOrder = a.SortOrder
        }).ToList(),
        SettingsJson = project.SettingsJson
    };

    private static LyricCueDto ToLyricDto(LyricCue cue) => new()
    {
        Index = cue.Index,
        Start = cue.Start,
        End = cue.End,
        Text = cue.Text,
        StartMs = cue.StartMs,
        EndMs = cue.EndMs,
        DurationMs = cue.DurationMs
    };

    private static LyricCue ToLyricCue(LyricCueDto dto, int index)
    {
        var startMs = dto.StartMs;
        var endMs = dto.EndMs;
        var start = new Timecode(startMs);
        var end = new Timecode(endMs);
        return new LyricCue
        {
            Index = index,
            Start = start.ToDisplayString(),
            End = end.ToDisplayString(),
            Text = dto.Text ?? string.Empty,
            StartMs = startMs,
            EndMs = endMs,
            DurationMs = endMs - startMs
        };
    }

    private static string GuessAudioContentType(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".mp3" => "audio/mpeg",
            ".wav" => "audio/wav",
            ".m4a" => "audio/mp4",
            ".aac" => "audio/aac",
            ".ogg" => "audio/ogg",
            ".flac" => "audio/flac",
            _ => "application/octet-stream"
        };
    }
}

public sealed class UpdateLyricsRequest
{
    public IReadOnlyList<LyricCueDto>? Lyrics { get; init; }
}

public sealed class CreateProjectRequest
{
    public string? Name { get; init; }
}

public sealed class ReorderAssetsRequest
{
    public required IReadOnlyList<ReorderAssetItem> Assets { get; init; }
}

public sealed class ReorderAssetItem
{
    public required string Id { get; init; }
    public int? SortOrder { get; init; }
}

public sealed class SrtUploadResponseDto
{
    public required string FileName { get; init; }
    public required string RelativePath { get; init; }
    public required bool IsValid { get; init; }
    public required int LyricCount { get; init; }
    public required IReadOnlyList<LyricCueDto> Lyrics { get; init; }
    public required IReadOnlyList<string> Issues { get; init; }
}

public sealed class LyricCueDto
{
    public required int Index { get; init; }
    public required string Start { get; init; }
    public required string End { get; init; }
    public required string Text { get; init; }
    public required long StartMs { get; init; }
    public required long EndMs { get; init; }
    public required long DurationMs { get; init; }
}

public sealed class ProjectSummaryDto
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public required string Status { get; init; }
    public string? AudioFileName { get; init; }
    public long? DurationMs { get; init; }
    public int? LyricCount { get; init; }
    public int? AssetCount { get; init; }
}

public sealed class ProjectDetailDto
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public string? AudioFileName { get; init; }
    public long? AudioDurationMs { get; init; }
    public string? SrtFileName { get; init; }
    public int? LyricCount { get; init; }
    public required IReadOnlyList<ProjectAssetDto> Assets { get; init; }
    public string? SettingsJson { get; init; }
}

public sealed class ProjectAssetDto
{
    public required string Id { get; init; }
    public required string FileName { get; init; }
    public required string Type { get; init; }
    public string? RelativePath { get; init; }
    public long? SourceDurationMs { get; init; }
    public int SortOrder { get; init; }
}
