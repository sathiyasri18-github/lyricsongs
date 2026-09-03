using Lyricsongs.Domain.Timeline;

namespace Lyricsongs.Domain.VideoPlan;

public sealed class BackgroundSegment
{
    public required string AssetId { get; init; }
    public required BackgroundAssetType Type { get; init; }
    public required long StartMs { get; init; }
    public required long EndMs { get; init; }
    public required BackgroundAnimation Animation { get; init; }

    public long DurationMs => EndMs - StartMs;

    /// <summary>Outgoing transition type to the next segment.</summary>
    public TransitionType TransitionOut { get; init; } = TransitionType.None;

    public long TransitionOutDurationMs { get; init; }

    public VideoPlaybackMode PlaybackMode { get; init; } = VideoPlaybackMode.Normal;
    public int LoopCount { get; init; } = 1;
    public long TrimStartMs { get; init; }
    public long? SourcePlayDurationMs { get; init; }
    public long? SourceDurationMs { get; init; }

    public double StartSeconds => StartMs / 1000.0;
    public double EndSeconds => EndMs / 1000.0;
}
