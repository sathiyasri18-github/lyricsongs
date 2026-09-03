namespace Lyricsongs.Domain.Timeline;

/// <summary>
/// A single contiguous background segment on the master timeline.
/// </summary>
public sealed class TimelineSegment
{
    public required int Sequence { get; init; }
    public required string AssetId { get; init; }
    public required string FileName { get; init; }
    public required BackgroundAssetType AssetType { get; init; }

    /// <summary>Segment start on the master timeline (ms).</summary>
    public required long StartMs { get; init; }

    /// <summary>Segment end on the master timeline (ms).</summary>
    public required long EndMs { get; init; }

    /// <summary>Display start time HH:MM:SS.mmm</summary>
    public required string Start { get; init; }

    /// <summary>Display end time HH:MM:SS.mmm</summary>
    public required string End { get; init; }

    /// <summary>
    /// Assigned slot duration including overlap budget for outgoing transition (ms).
    /// Sum of slot durations minus transition overlaps equals audio duration.
    /// </summary>
    public required long SlotDurationMs { get; init; }

    /// <summary>Outgoing crossfade/overlap duration to the next segment (0 for last segment).</summary>
    public required long TransitionOutDurationMs { get; init; }

    public VideoPlaybackMode PlaybackMode { get; init; } = VideoPlaybackMode.Normal;

    /// <summary>How many times the source video loops (1 = no loop).</summary>
    public int LoopCount { get; init; } = 1;

    /// <summary>Trim start offset within the source video (ms).</summary>
    public long TrimStartMs { get; init; }

    /// <summary>Duration used from the source video per loop iteration (ms).</summary>
    public long? SourcePlayDurationMs { get; init; }

    public long? SourceDurationMs { get; init; }
}
