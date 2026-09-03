using Lyricsongs.Application.Subtitles;
using Lyricsongs.Domain.Subtitles;

namespace Lyricsongs.Infrastructure.Subtitles;

/// <summary>
/// Validates lyric cues for overlaps, gaps, duplicate indexes, and related timeline issues.
/// </summary>
public sealed class SrtValidationService : ISrtValidationService
{
    /// <summary>Gaps larger than this emit a warning (default 10 seconds).</summary>
    public long LargeGapWarningThresholdMs { get; init; } = 10_000;

    public SrtValidationResult Validate(IReadOnlyList<LyricCue> lyrics)
    {
        ArgumentNullException.ThrowIfNull(lyrics);

        var issues = new List<SrtValidationIssue>();
        var gaps = new List<LyricGap>();
        var overlaps = new List<LyricOverlap>();

        if (lyrics.Count == 0)
        {
            return new SrtValidationResult
            {
                Issues = issues,
                Gaps = gaps,
                Overlaps = overlaps
            };
        }

        DetectDuplicateIndexes(lyrics, issues);

        var ordered = lyrics
            .Select((cue, originalOrder) => (cue, originalOrder))
            .OrderBy(x => x.cue.StartMs)
            .ThenBy(x => x.cue.EndMs)
            .ThenBy(x => x.originalOrder)
            .Select(x => x.cue)
            .ToList();

        for (var i = 0; i < ordered.Count - 1; i++)
        {
            var current = ordered[i];
            var next = ordered[i + 1];
            var gapMs = next.StartMs - current.EndMs;

            if (gapMs < 0)
            {
                var overlapMs = -gapMs;
                overlaps.Add(new LyricOverlap
                {
                    FirstIndex = current.Index,
                    SecondIndex = next.Index,
                    OverlapMs = overlapMs,
                    FirstEnd = current.End,
                    SecondStart = next.Start
                });

                issues.Add(new SrtValidationIssue
                {
                    Code = SrtValidationCode.OverlappingCues,
                    Message =
                        $"Cue {current.Index} ({current.Start} --> {current.End}) overlaps cue {next.Index} ({next.Start} --> {next.End}) by {overlapMs} ms.",
                    Severity = SrtValidationSeverity.Error,
                    CueIndex = current.Index,
                    RelatedCueIndex = next.Index,
                    LineNumber = current.SourceLineNumber
                });
            }
            else
            {
                gaps.Add(new LyricGap
                {
                    FromIndex = current.Index,
                    ToIndex = next.Index,
                    GapMs = gapMs,
                    FromEnd = current.End,
                    ToStart = next.Start
                });

                if (gapMs >= LargeGapWarningThresholdMs)
                {
                    issues.Add(new SrtValidationIssue
                    {
                        Code = SrtValidationCode.LargeGap,
                        Message =
                            $"Large gap of {gapMs} ms between cue {current.Index} (ends {current.End}) and cue {next.Index} (starts {next.Start}).",
                        Severity = SrtValidationSeverity.Warning,
                        CueIndex = current.Index,
                        RelatedCueIndex = next.Index,
                        LineNumber = current.SourceLineNumber
                    });
                }
            }
        }

        // Also detect overlaps that are not adjacent in start-time order
        // (e.g. cue A spans over B and C). Adjacent check catches most;
        // nested/span overlaps need pairwise for non-adjacent.
        DetectNonAdjacentOverlaps(ordered, overlaps, issues);

        return new SrtValidationResult
        {
            Issues = issues,
            Gaps = gaps,
            Overlaps = overlaps
        };
    }

    private static void DetectDuplicateIndexes(IReadOnlyList<LyricCue> lyrics, List<SrtValidationIssue> issues)
    {
        var duplicates = lyrics
            .GroupBy(c => c.Index)
            .Where(g => g.Count() > 1);

        foreach (var group in duplicates)
        {
            issues.Add(new SrtValidationIssue
            {
                Code = SrtValidationCode.DuplicateIndex,
                Message = $"Duplicate subtitle index {group.Key} appears {group.Count()} times.",
                Severity = SrtValidationSeverity.Warning,
                CueIndex = group.Key,
                LineNumber = group.First().SourceLineNumber
            });
        }
    }

    private static void DetectNonAdjacentOverlaps(
        IReadOnlyList<LyricCue> orderedByStart,
        List<LyricOverlap> overlaps,
        List<SrtValidationIssue> issues)
    {
        var existingPairs = new HashSet<(int, int)>(
            overlaps.Select(o => (Math.Min(o.FirstIndex, o.SecondIndex), Math.Max(o.FirstIndex, o.SecondIndex))));

        for (var i = 0; i < orderedByStart.Count; i++)
        {
            var a = orderedByStart[i];
            for (var j = i + 2; j < orderedByStart.Count; j++)
            {
                var b = orderedByStart[j];
                if (b.StartMs >= a.EndMs)
                {
                    break;
                }

                var pair = (Math.Min(a.Index, b.Index), Math.Max(a.Index, b.Index));
                if (!existingPairs.Add(pair))
                {
                    continue;
                }

                var overlapMs = Math.Min(a.EndMs, b.EndMs) - Math.Max(a.StartMs, b.StartMs);
                if (overlapMs <= 0)
                {
                    continue;
                }

                overlaps.Add(new LyricOverlap
                {
                    FirstIndex = a.Index,
                    SecondIndex = b.Index,
                    OverlapMs = overlapMs,
                    FirstEnd = a.End,
                    SecondStart = b.Start
                });

                issues.Add(new SrtValidationIssue
                {
                    Code = SrtValidationCode.OverlappingCues,
                    Message =
                        $"Cue {a.Index} ({a.Start} --> {a.End}) overlaps cue {b.Index} ({b.Start} --> {b.End}) by {overlapMs} ms.",
                    Severity = SrtValidationSeverity.Error,
                    CueIndex = a.Index,
                    RelatedCueIndex = b.Index,
                    LineNumber = a.SourceLineNumber
                });
            }
        }
    }
}
