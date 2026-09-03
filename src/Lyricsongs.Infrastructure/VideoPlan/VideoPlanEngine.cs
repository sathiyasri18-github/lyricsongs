using Lyricsongs.Application.VideoPlan;
using Lyricsongs.Domain.VideoPlan;

namespace Lyricsongs.Infrastructure.VideoPlan;

/// <summary>
/// Deterministic Video Plan Engine — no AI, no FFmpeg commands.
/// Orchestrates background planning, lyric style resolution, and validation.
/// </summary>
public sealed class VideoPlanEngine : IVideoPlanEngine
{
    private readonly IBackgroundPlanner _backgroundPlanner;
    private readonly ILyricStyleManager _lyricStyleManager;
    private readonly IVideoPlanValidator _validator;

    private static readonly Dictionary<VideoResolutionPreset, (int W, int H, VideoAspectRatio Ratio)> Resolutions = new()
    {
        [VideoResolutionPreset.FullHd1080p] = (1920, 1080, VideoAspectRatio.Landscape16x9),
        [VideoResolutionPreset.Hd720p] = (1280, 720, VideoAspectRatio.Landscape16x9),
        [VideoResolutionPreset.Vertical1080] = (1080, 1920, VideoAspectRatio.Portrait9x16),
        [VideoResolutionPreset.Vertical720] = (720, 1280, VideoAspectRatio.Portrait9x16),
        [VideoResolutionPreset.Square1080] = (1080, 1080, VideoAspectRatio.Square1x1)
    };

    public VideoPlanEngine(
        IBackgroundPlanner backgroundPlanner,
        ILyricStyleManager lyricStyleManager,
        IVideoPlanValidator validator)
    {
        _backgroundPlanner = backgroundPlanner;
        _lyricStyleManager = lyricStyleManager;
        _validator = validator;
    }

    public VideoPlanBuildResult Build(VideoPlanRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateRequestBasics(request);

        var backgrounds = _backgroundPlanner.Plan(
            request.AudioDurationMs,
            request.Assets,
            request.Settings);

        var lyrics = _lyricStyleManager.Resolve(request.Settings);
        var output = BuildOutput(request.Settings);
        var transition = CloneTransition(request.Settings.Transition);

        var plan = new Domain.VideoPlan.VideoPlan
        {
            PlanId = CreateDeterministicPlanId(request),
            AudioDurationMs = request.AudioDurationMs,
            Output = output,
            Backgrounds = backgrounds,
            Lyrics = lyrics,
            Transition = transition,
            Subtitles = request.Lyrics.ToList(),
            OrderedAssetIds = backgrounds.Select(b => b.AssetId).ToList(),
            PlanningMode = request.Settings.PlanningMode,
            RandomSeed = request.Settings.RandomSeed
        };

        var validation = _validator.Validate(plan, request.Assets, request);

        if (!validation.IsValid)
        {
            throw new VideoPlanException(
                $"Video plan validation failed with {validation.Errors.Count()} error(s): " +
                string.Join("; ", validation.Errors.Select(e => e.Message)),
                validation);
        }

        return new VideoPlanBuildResult
        {
            Plan = plan,
            Validation = validation
        };
    }

    public VideoPlanBuildResult ApplyOverrides(VideoPlanRequest request, VideoPlanSettings overrides)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(overrides);

        var merged = MergeSettings(request.Settings, overrides);
        var updatedRequest = new VideoPlanRequest
        {
            AudioDurationMs = request.AudioDurationMs,
            Lyrics = request.Lyrics,
            Assets = request.Assets,
            Settings = merged
        };

        return Build(updatedRequest);
    }

    private static void ValidateRequestBasics(VideoPlanRequest request)
    {
        if (request.AudioDurationMs <= 0)
        {
            throw new VideoPlanException("Audio duration must be greater than zero.");
        }

        if (request.Assets.Count == 0)
        {
            throw new VideoPlanException("At least one background asset is required.");
        }
    }

    private static VideoOutputSettings BuildOutput(VideoPlanSettings settings)
    {
        if (!Resolutions.TryGetValue(settings.ResolutionPreset, out var res))
        {
            throw new VideoPlanException($"Unsupported resolution preset '{settings.ResolutionPreset}'.");
        }

        return new VideoOutputSettings
        {
            Width = res.W,
            Height = res.H,
            Fps = settings.Fps,
            VideoCodec = "libx264",
            AudioCodec = "aac",
            Format = "mp4",
            AspectRatio = res.Ratio,
            ResolutionPreset = settings.ResolutionPreset
        };
    }

    private static TransitionSettings CloneTransition(TransitionSettings source) => new()
    {
        Type = source.Type,
        DurationMs = source.Type == TransitionType.None ? 0 : source.DurationMs
    };

    private static VideoPlanSettings MergeSettings(VideoPlanSettings original, VideoPlanSettings overrides) => new()
    {
        PlanningMode = overrides.PlanningMode,
        RandomSeed = overrides.RandomSeed,
        Transition = overrides.Transition,
        MinSegmentDurationMs = overrides.MinSegmentDurationMs > 0
            ? overrides.MinSegmentDurationMs
            : original.MinSegmentDurationMs,
        LyricStyle = overrides.LyricStyle,
        ResolutionPreset = overrides.ResolutionPreset,
        Fps = overrides.Fps,
        DefaultImageAnimation = overrides.DefaultImageAnimation,
        CycleImageAnimations = overrides.CycleImageAnimations,
        FontOverride = overrides.FontOverride ?? original.FontOverride,
        FontSizeOverride = overrides.FontSizeOverride ?? original.FontSizeOverride,
        PositionOverride = overrides.PositionOverride ?? original.PositionOverride,
        AlignmentOverride = overrides.AlignmentOverride ?? original.AlignmentOverride,
        TextColorOverride = overrides.TextColorOverride ?? original.TextColorOverride,
        TextColor2Override = overrides.TextColor2Override ?? original.TextColor2Override,
        TextColor3Override = overrides.TextColor3Override ?? original.TextColor3Override,
        HighlightColorOverride = overrides.HighlightColorOverride ?? original.HighlightColorOverride,
        OutlineOverride = overrides.OutlineOverride ?? original.OutlineOverride,
        ShadowOverride = overrides.ShadowOverride ?? original.ShadowOverride,
        FadeInMsOverride = overrides.FadeInMsOverride ?? original.FadeInMsOverride,
        FadeOutMsOverride = overrides.FadeOutMsOverride ?? original.FadeOutMsOverride,
        AssetOrderOverride = overrides.AssetOrderOverride ?? original.AssetOrderOverride,
        ManualBackgrounds = overrides.ManualBackgrounds ?? original.ManualBackgrounds
    };

    /// <summary>
    /// Stable plan id derived from inputs (no randomness except explicit seed).
    /// </summary>
    private static string CreateDeterministicPlanId(VideoPlanRequest request)
    {
        var assetPart = string.Join(",", request.Assets
            .OrderBy(a => a.AssetId, StringComparer.Ordinal)
            .Select(a => $"{a.AssetId}:{a.Type}:{a.SourceDurationMs ?? 0}"));

        var lyricPart = string.Join(",", request.Lyrics
            .OrderBy(l => l.Index)
            .Select(l => $"{l.Index}:{l.StartMs}:{l.EndMs}:{l.Text.Length}"));

        var key =
            $"{request.AudioDurationMs}|{request.Settings.PlanningMode}|{request.Settings.RandomSeed}|" +
            $"{request.Settings.LyricStyle}|{request.Settings.ResolutionPreset}|{request.Settings.Fps}|" +
            $"{request.Settings.Transition.Type}|{request.Settings.Transition.DurationMs}|{assetPart}|{lyricPart}";

        var hash = key.GetHashCode(StringComparison.Ordinal);
        return $"plan_{unchecked((uint)hash):x8}";
    }
}
