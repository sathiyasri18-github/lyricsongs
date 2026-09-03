namespace Lyricsongs.Domain.Subtitles;

/// <summary>
/// A single lyric subtitle cue parsed from an SRT file.
/// </summary>
public sealed class LyricCue
{
    public required int Index { get; init; }

    /// <summary>Canonical start time: HH:MM:SS.mmm</summary>
    public required string Start { get; init; }

    /// <summary>Canonical end time: HH:MM:SS.mmm</summary>
    public required string End { get; init; }

    /// <summary>Lyric text (UTF-8 / Unicode). Multiline cues use '\n'.</summary>
    public required string Text { get; init; }

    public long StartMs { get; init; }
    public long EndMs { get; init; }

    /// <summary>Cue duration in milliseconds (EndMs - StartMs).</summary>
    public long DurationMs { get; init; }

    /// <summary>
    /// Gap in milliseconds until the next cue starts.
    /// Null when this is the last cue. Negative when cues overlap.
    /// </summary>
    public long? GapToNextMs { get; init; }

    public int? SourceLineNumber { get; init; }
}
