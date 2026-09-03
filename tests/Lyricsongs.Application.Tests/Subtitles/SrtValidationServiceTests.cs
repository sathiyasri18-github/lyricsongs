using Lyricsongs.Domain.Subtitles;
using Lyricsongs.Infrastructure.Subtitles;

namespace Lyricsongs.Application.Tests.Subtitles;

public class SrtValidationServiceTests
{
    private readonly SrtValidationService _validator = new();

    [Fact]
    public void Validate_DetectsOverlap()
    {
        var cues = new List<LyricCue>
        {
            CreateCue(1, 0, 5000, "A"),
            CreateCue(2, 4000, 8000, "B")
        };

        var result = _validator.Validate(cues);

        Assert.Single(result.Overlaps);
        Assert.Equal(1000, result.Overlaps[0].OverlapMs);
        Assert.Contains(result.Issues, i => i.Code == SrtValidationCode.OverlappingCues);
    }

    [Fact]
    public void Validate_CalculatesGaps()
    {
        var cues = new List<LyricCue>
        {
            CreateCue(1, 0, 2000, "A"),
            CreateCue(2, 3500, 5000, "B")
        };

        var result = _validator.Validate(cues);

        Assert.Single(result.Gaps);
        Assert.Equal(1500, result.Gaps[0].GapMs);
        Assert.Equal(1, result.Gaps[0].FromIndex);
        Assert.Equal(2, result.Gaps[0].ToIndex);
    }

    [Fact]
    public void Validate_LargeGap_EmitsWarning()
    {
        var cues = new List<LyricCue>
        {
            CreateCue(1, 0, 1000, "A"),
            CreateCue(2, 20000, 21000, "B")
        };

        var result = _validator.Validate(cues);

        Assert.Contains(result.Issues, i =>
            i.Code == SrtValidationCode.LargeGap &&
            i.Severity == SrtValidationSeverity.Warning);
    }

    [Fact]
    public void Validate_DuplicateIndexes_EmitsWarning()
    {
        var cues = new List<LyricCue>
        {
            CreateCue(1, 0, 1000, "A"),
            CreateCue(1, 2000, 3000, "B")
        };

        var result = _validator.Validate(cues);

        Assert.Contains(result.Issues, i => i.Code == SrtValidationCode.DuplicateIndex);
    }

    [Fact]
    public void Validate_NestedOverlap_Detected()
    {
        var cues = new List<LyricCue>
        {
            CreateCue(1, 0, 10000, "Long"),
            CreateCue(2, 2000, 3000, "Nested"),
            CreateCue(3, 12000, 13000, "After")
        };

        var result = _validator.Validate(cues);

        Assert.Contains(result.Overlaps, o => o.FirstIndex == 1 && o.SecondIndex == 2);
    }

    private static LyricCue CreateCue(int index, long startMs, long endMs, string text)
    {
        var start = new Timecode(startMs);
        var end = new Timecode(endMs);
        return new LyricCue
        {
            Index = index,
            Start = start.ToDisplayString(),
            End = end.ToDisplayString(),
            Text = text,
            StartMs = startMs,
            EndMs = endMs,
            DurationMs = endMs - startMs,
            GapToNextMs = null,
            SourceLineNumber = index
        };
    }
}
