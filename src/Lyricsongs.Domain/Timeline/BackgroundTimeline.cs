namespace Lyricsongs.Domain.Timeline;

public sealed class BackgroundTimeline
{
    public required long AudioDurationMs { get; init; }
    public required string AudioDuration { get; init; }
    public required long AvailableVideoDurationMs { get; init; }
    public required long TransitionDurationMs { get; init; }
    public required AssetOrderingMode OrderingMode { get; init; }
    public required int OrderingSeed { get; init; }
    public required IReadOnlyList<string> OrderedAssetIds { get; init; }
    public required IReadOnlyList<TimelineSegment> Segments { get; init; }
    public required BackgroundTimelineSummary Summary { get; init; }
}

public sealed class BackgroundTimelineSummary
{
    public required int SegmentCount { get; init; }
    public required int ImageCount { get; init; }
    public required int VideoCount { get; init; }
    public required long TotalSlotDurationMs { get; init; }
    public required long TotalTransitionOverlapMs { get; init; }
    public required long TimelineEndMs { get; init; }
}
