using System.Text;
using Lyricsongs.Domain.Subtitles;
using Lyricsongs.Infrastructure.Subtitles;

namespace Lyricsongs.Application.Tests.Subtitles;

public class SrtParserTests
{
    private readonly SrtParser _parser = new();

    [Fact]
    public void Parse_TamilSample_ProducesExpectedCues()
    {
        const string srt = """
            1
            00:00:05,000 --> 00:00:09,000
            என்னை விட்டு போகாதே

            2
            00:00:09,000 --> 00:00:13,500
            என் உயிரே நீயே
            """;

        var result = _parser.Parse(srt);

        Assert.Empty(result.ParseIssues.Where(i => i.Severity == SrtValidationSeverity.Error));
        Assert.Equal(2, result.Lyrics.Count);

        Assert.Equal(1, result.Lyrics[0].Index);
        Assert.Equal("00:00:05.000", result.Lyrics[0].Start);
        Assert.Equal("00:00:09.000", result.Lyrics[0].End);
        Assert.Equal("என்னை விட்டு போகாதே", result.Lyrics[0].Text);
        Assert.Equal(4000, result.Lyrics[0].DurationMs);

        Assert.Equal(2, result.Lyrics[1].Index);
        Assert.Equal("00:00:09.000", result.Lyrics[1].Start);
        Assert.Equal("00:00:13.500", result.Lyrics[1].End);
        Assert.Equal("என் உயிரே நீயே", result.Lyrics[1].Text);
        Assert.Equal(4500, result.Lyrics[1].DurationMs);
    }

    [Fact]
    public void Parse_EnglishMultiline_JoinsWithNewline()
    {
        const string srt = """
            1
            00:00:01,000 --> 00:00:04,000
            Line one
            Line two
            """;

        var result = _parser.Parse(srt);

        Assert.Single(result.Lyrics);
        Assert.Equal("Line one\nLine two", result.Lyrics[0].Text);
    }

    [Fact]
    public void Parse_WithBomAndCrlf_Succeeds()
    {
        var content = "1\r\n00:00:01,000 --> 00:00:02,000\r\nHello\r\n";
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(content)).ToArray();
        using var stream = new MemoryStream(bytes);

        var result = _parser.Parse(stream);

        Assert.Single(result.Lyrics);
        Assert.Equal("Hello", result.Lyrics[0].Text);
    }

    [Fact]
    public void Parse_DotTimecodes_Supported()
    {
        const string srt = """
            1
            00:00:01.000 --> 00:00:02.500
            Dot style
            """;

        var result = _parser.Parse(srt);

        Assert.Single(result.Lyrics);
        Assert.Equal("00:00:01.000", result.Lyrics[0].Start);
        Assert.Equal("00:00:02.500", result.Lyrics[0].End);
    }

    [Fact]
    public void Parse_EmptyContent_ReturnsError()
    {
        var result = _parser.Parse("   ");

        Assert.Empty(result.Lyrics);
        Assert.Contains(result.ParseIssues, i => i.Code == SrtValidationCode.EmptyFile);
    }

    [Fact]
    public void Parse_InvalidTimecode_SkipsBlockGracefully()
    {
        const string srt = """
            1
            00:00:99,000 --> 00:01:00,000
            Bad seconds

            2
            00:01:00,000 --> 00:01:02,000
            Good cue
            """;

        var result = _parser.Parse(srt);

        Assert.Single(result.Lyrics);
        Assert.Equal(2, result.Lyrics[0].Index);
        Assert.Contains(result.ParseIssues, i => i.Code == SrtValidationCode.InvalidTimecode);
    }

    [Fact]
    public void Parse_MissingTimestamp_ReportsError()
    {
        const string srt = """
            1
            Just some text without timing
            """;

        var result = _parser.Parse(srt);

        Assert.Empty(result.Lyrics);
        Assert.Contains(result.ParseIssues, i => i.Code == SrtValidationCode.MissingTimestamp);
    }

    [Fact]
    public void Parse_EndBeforeStart_ReportsInvalidTimeRange()
    {
        const string srt = """
            1
            00:00:10,000 --> 00:00:05,000
            Backwards
            """;

        var result = _parser.Parse(srt);

        Assert.Empty(result.Lyrics);
        Assert.Contains(result.ParseIssues, i => i.Code == SrtValidationCode.InvalidTimeRange);
    }

    [Fact]
    public void Parse_MissingText_ReportsWarningAndKeepsCue()
    {
        const string srt = """
            1
            00:00:01,000 --> 00:00:02,000

            """;

        var result = _parser.Parse(srt);

        Assert.Single(result.Lyrics);
        Assert.Equal(string.Empty, result.Lyrics[0].Text);
        Assert.Contains(result.ParseIssues, i => i.Code == SrtValidationCode.MissingText);
        Assert.All(result.ParseIssues.Where(i => i.Code == SrtValidationCode.MissingText),
            i => Assert.Equal(SrtValidationSeverity.Warning, i.Severity));
    }

    [Fact]
    public void Parse_MissingIndex_AssignsSequentialIndex()
    {
        const string srt = """
            00:00:01,000 --> 00:00:02,000
            No index here
            """;

        var result = _parser.Parse(srt);

        Assert.Single(result.Lyrics);
        Assert.Equal(1, result.Lyrics[0].Index);
        Assert.Contains(result.ParseIssues, i => i.Code == SrtValidationCode.MissingIndex);
    }

    [Fact]
    public void Parse_MalformedBlock_DoesNotThrow_ContinuesParsing()
    {
        const string srt = """
            not-a-valid-block

            1
            00:00:05,000 --> 00:00:06,000
            Recovered
            """;

        var result = _parser.Parse(srt);

        Assert.Single(result.Lyrics);
        Assert.Equal("Recovered", result.Lyrics[0].Text);
        Assert.NotEmpty(result.ParseIssues);
    }
}
