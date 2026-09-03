using Lyricsongs.Domain.Timeline;

namespace Lyricsongs.Application.Timeline;

public sealed class BackgroundTimelineRequest
{
    /// <summary>Total audio duration in milliseconds.</summary>
    public required long AudioDurationMs { get; init; }

    public required IReadOnlyList<BackgroundAsset> Assets { get; init; }

    public AssetOrderingMode OrderingMode { get; init; } = AssetOrderingMode.Manual;

    /// <summary>Deterministic seed for automatic ordering. Default 42.</summary>
    public int OrderingSeed { get; init; } = 42;

    /// <summary>Crossfade overlap between consecutive segments (ms). Default 500.</summary>
    public long TransitionDurationMs { get; init; } = 500;

    /// <summary>Minimum segment slot duration (ms). Default 3000.</summary>
    public long MinSegmentDurationMs { get; init; } = 3_000;
}

public sealed class BackgroundTimelineBuildResult
{
    public required BackgroundTimeline Timeline { get; init; }
    public required TimelineValidationResult Validation { get; init; }
}

/// <summary>
/// Builds a deterministic background timeline covering the full audio duration without gaps.
/// </summary>
public interface IBackgroundTimelineService
{
    BackgroundTimelineBuildResult Build(BackgroundTimelineRequest request);
}
