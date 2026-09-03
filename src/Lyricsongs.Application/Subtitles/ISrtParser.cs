using Lyricsongs.Domain.Subtitles;

namespace Lyricsongs.Application.Subtitles;

/// <summary>
/// Parses SRT subtitle content into structured lyric cues.
/// Does not perform cross-cue semantic validation (overlaps, gaps) — that belongs to <see cref="ISrtValidationService"/>.
/// </summary>
public interface ISrtParser
{
    /// <summary>
    /// Parses UTF-8 SRT text (BOM and any line endings allowed).
    /// Malformed blocks produce issues and are skipped when possible.
    /// </summary>
    SrtParserResult Parse(string srtContent);

    /// <summary>
    /// Parses an SRT stream as UTF-8 (BOM-aware).
    /// </summary>
    SrtParserResult Parse(Stream stream);
}

public sealed class SrtParserResult
{
    public required IReadOnlyList<LyricCue> Lyrics { get; init; }
    public required IReadOnlyList<SrtValidationIssue> ParseIssues { get; init; }
}
