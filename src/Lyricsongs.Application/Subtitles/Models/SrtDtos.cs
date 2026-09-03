using Lyricsongs.Domain.Subtitles;

namespace Lyricsongs.Application.Subtitles.Models;

/// <summary>
/// API-facing lyric cue matching the required JSON shape, plus timing analytics.
/// </summary>
public sealed class LyricCueDto
{
    public int Index { get; init; }
    public string Start { get; init; } = string.Empty;
    public string End { get; init; } = string.Empty;
    public string Text { get; init; } = string.Empty;
    public long DurationMs { get; init; }
    public long? GapToNextMs { get; init; }
}

public sealed class SrtValidationIssueDto
{
    public string Code { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public int? CueIndex { get; init; }
    public int? RelatedCueIndex { get; init; }
    public int? LineNumber { get; init; }
}

public sealed class LyricGapDto
{
    public int FromIndex { get; init; }
    public int ToIndex { get; init; }
    public long GapMs { get; init; }
    public string FromEnd { get; init; } = string.Empty;
    public string ToStart { get; init; } = string.Empty;
}

public sealed class LyricOverlapDto
{
    public int FirstIndex { get; init; }
    public int SecondIndex { get; init; }
    public long OverlapMs { get; init; }
    public string FirstEnd { get; init; } = string.Empty;
    public string SecondStart { get; init; } = string.Empty;
}

public sealed class SrtParseSummaryDto
{
    public int CueCount { get; init; }
    public int ErrorCount { get; init; }
    public int WarningCount { get; init; }
    public int OverlapCount { get; init; }
    public int GapCount { get; init; }
    public long? FirstCueStartMs { get; init; }
    public long? LastCueEndMs { get; init; }
    public long? TotalCoveredDurationMs { get; init; }
}

public sealed class SrtProcessResponseDto
{
    public bool IsValid { get; init; }
    public bool HasWarnings { get; init; }
    public IReadOnlyList<LyricCueDto> Lyrics { get; init; } = [];
    public IReadOnlyList<SrtValidationIssueDto> Errors { get; init; } = [];
    public IReadOnlyList<SrtValidationIssueDto> Warnings { get; init; } = [];
    public IReadOnlyList<LyricGapDto> Gaps { get; init; } = [];
    public IReadOnlyList<LyricOverlapDto> Overlaps { get; init; } = [];
    public SrtParseSummaryDto Summary { get; init; } = new();
}

public static class SrtDtoMapper
{
    public static SrtProcessResponseDto ToResponse(SrtParseResult result)
    {
        var errors = result.Issues
            .Where(i => i.Severity == SrtValidationSeverity.Error)
            .Select(ToIssueDto)
            .ToList();

        var warnings = result.Issues
            .Where(i => i.Severity == SrtValidationSeverity.Warning)
            .Select(ToIssueDto)
            .ToList();

        return new SrtProcessResponseDto
        {
            IsValid = result.IsValid,
            HasWarnings = result.HasWarnings,
            Lyrics = result.Lyrics.Select(ToCueDto).ToList(),
            Errors = errors,
            Warnings = warnings,
            Gaps = result.Gaps.Select(g => new LyricGapDto
            {
                FromIndex = g.FromIndex,
                ToIndex = g.ToIndex,
                GapMs = g.GapMs,
                FromEnd = g.FromEnd,
                ToStart = g.ToStart
            }).ToList(),
            Overlaps = result.Overlaps.Select(o => new LyricOverlapDto
            {
                FirstIndex = o.FirstIndex,
                SecondIndex = o.SecondIndex,
                OverlapMs = o.OverlapMs,
                FirstEnd = o.FirstEnd,
                SecondStart = o.SecondStart
            }).ToList(),
            Summary = new SrtParseSummaryDto
            {
                CueCount = result.Summary.CueCount,
                ErrorCount = result.Summary.ErrorCount,
                WarningCount = result.Summary.WarningCount,
                OverlapCount = result.Summary.OverlapCount,
                GapCount = result.Summary.GapCount,
                FirstCueStartMs = result.Summary.FirstCueStartMs,
                LastCueEndMs = result.Summary.LastCueEndMs,
                TotalCoveredDurationMs = result.Summary.TotalCoveredDurationMs
            }
        };
    }

    private static LyricCueDto ToCueDto(LyricCue cue) => new()
    {
        Index = cue.Index,
        Start = cue.Start,
        End = cue.End,
        Text = cue.Text,
        DurationMs = cue.DurationMs,
        GapToNextMs = cue.GapToNextMs
    };

    private static SrtValidationIssueDto ToIssueDto(SrtValidationIssue issue) => new()
    {
        Code = issue.Code,
        Message = issue.Message,
        Severity = issue.Severity.ToString(),
        CueIndex = issue.CueIndex,
        RelatedCueIndex = issue.RelatedCueIndex,
        LineNumber = issue.LineNumber
    };
}
