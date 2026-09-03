namespace Lyricsongs.Domain.Subtitles;

/// <summary>
/// Overlap between two lyric cues where the earlier cue ends after the later cue starts.
/// </summary>
public sealed class LyricOverlap
{
    public required int FirstIndex { get; init; }
    public required int SecondIndex { get; init; }
    public required long OverlapMs { get; init; }
    public required string FirstEnd { get; init; }
    public required string SecondStart { get; init; }
}
