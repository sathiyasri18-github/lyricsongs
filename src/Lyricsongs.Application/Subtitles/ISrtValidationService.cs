using Lyricsongs.Domain.Subtitles;

namespace Lyricsongs.Application.Subtitles;

/// <summary>
/// Validates parsed lyric cues for overlaps, gaps, duplicate indexes, and related issues.
/// </summary>
public interface ISrtValidationService
{
    SrtValidationResult Validate(IReadOnlyList<LyricCue> lyrics);
}

public sealed class SrtValidationResult
{
    public required IReadOnlyList<SrtValidationIssue> Issues { get; init; }
    public required IReadOnlyList<LyricGap> Gaps { get; init; }
    public required IReadOnlyList<LyricOverlap> Overlaps { get; init; }
}
