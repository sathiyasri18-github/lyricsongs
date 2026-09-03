namespace Lyricsongs.Domain.Subtitles;

/// <summary>
/// Gap between two consecutive lyric cues.
/// </summary>
public sealed class LyricGap
{
    public required int FromIndex { get; init; }
    public required int ToIndex { get; init; }
    public required long GapMs { get; init; }
    public required string FromEnd { get; init; }
    public required string ToStart { get; init; }
}
