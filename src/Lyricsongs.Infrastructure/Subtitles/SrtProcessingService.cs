using Lyricsongs.Application.Subtitles;
using Lyricsongs.Domain.Subtitles;

namespace Lyricsongs.Infrastructure.Subtitles;

/// <summary>
/// Parses SRT content, validates cues, computes durations/gaps, and builds the final result.
/// </summary>
public sealed class SrtProcessingService : ISrtProcessingService
{
    private readonly ISrtParser _parser;
    private readonly ISrtValidationService _validationService;

    public SrtProcessingService(ISrtParser parser, ISrtValidationService validationService)
    {
        _parser = parser;
        _validationService = validationService;
    }

    public SrtParseResult Process(string srtContent)
    {
        var parseResult = _parser.Parse(srtContent);
        return BuildResult(parseResult);
    }

    public SrtParseResult Process(Stream stream)
    {
        var parseResult = _parser.Parse(stream);
        return BuildResult(parseResult);
    }

    private SrtParseResult BuildResult(SrtParserResult parseResult)
    {
        var ordered = parseResult.Lyrics
            .Select((cue, order) => (cue, order))
            .OrderBy(x => x.cue.StartMs)
            .ThenBy(x => x.order)
            .Select(x => x.cue)
            .ToList();

        var validation = _validationService.Validate(ordered);

        // Enrich cues with GapToNextMs based on chronological order.
        var enriched = new List<LyricCue>(ordered.Count);
        for (var i = 0; i < ordered.Count; i++)
        {
            var cue = ordered[i];
            long? gapToNext = null;

            if (i < ordered.Count - 1)
            {
                gapToNext = ordered[i + 1].StartMs - cue.EndMs;
            }

            enriched.Add(new LyricCue
            {
                Index = cue.Index,
                Start = cue.Start,
                End = cue.End,
                Text = cue.Text,
                StartMs = cue.StartMs,
                EndMs = cue.EndMs,
                DurationMs = cue.DurationMs,
                GapToNextMs = gapToNext,
                SourceLineNumber = cue.SourceLineNumber
            });
        }

        var allIssues = parseResult.ParseIssues
            .Concat(validation.Issues)
            .ToList();

        var summary = new SrtParseSummary
        {
            CueCount = enriched.Count,
            ErrorCount = allIssues.Count(i => i.Severity == SrtValidationSeverity.Error),
            WarningCount = allIssues.Count(i => i.Severity == SrtValidationSeverity.Warning),
            OverlapCount = validation.Overlaps.Count,
            GapCount = validation.Gaps.Count,
            FirstCueStartMs = enriched.Count > 0 ? enriched[0].StartMs : null,
            LastCueEndMs = enriched.Count > 0 ? enriched[^1].EndMs : null,
            TotalCoveredDurationMs = enriched.Count > 0
                ? enriched.Sum(c => c.DurationMs)
                : null
        };

        return new SrtParseResult
        {
            Lyrics = enriched,
            Issues = allIssues,
            Gaps = validation.Gaps,
            Overlaps = validation.Overlaps,
            Summary = summary
        };
    }
}
