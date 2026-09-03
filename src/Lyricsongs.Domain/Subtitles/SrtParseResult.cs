namespace Lyricsongs.Domain.Subtitles;

public sealed class SrtParseResult
{
    public required IReadOnlyList<LyricCue> Lyrics { get; init; }
    public required IReadOnlyList<SrtValidationIssue> Issues { get; init; }
    public required IReadOnlyList<LyricGap> Gaps { get; init; }
    public required IReadOnlyList<LyricOverlap> Overlaps { get; init; }
    public required SrtParseSummary Summary { get; init; }

    public bool IsValid => Issues.All(i => i.Severity != SrtValidationSeverity.Error);
    public bool HasWarnings => Issues.Any(i => i.Severity == SrtValidationSeverity.Warning);
}

public sealed class SrtParseSummary
{
    public required int CueCount { get; init; }
    public required int ErrorCount { get; init; }
    public required int WarningCount { get; init; }
    public required int OverlapCount { get; init; }
    public required int GapCount { get; init; }
    public long? FirstCueStartMs { get; init; }
    public long? LastCueEndMs { get; init; }
    public long? TotalCoveredDurationMs { get; init; }
}
