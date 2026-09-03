using Lyricsongs.Application.Subtitles.Models;
using Lyricsongs.Domain.Subtitles;
using Lyricsongs.Infrastructure.Subtitles;

namespace Lyricsongs.Application.Tests.Subtitles;

public class SrtProcessingServiceTests
{
    private readonly SrtProcessingService _service = new(new SrtParser(), new SrtValidationService());

    [Fact]
    public void Process_TamilSample_MatchesRequiredShape()
    {
        const string srt = """
            1
            00:00:05,000 --> 00:00:09,000
            என்னை விட்டு போகாதே

            2
            00:00:09,000 --> 00:00:13,500
            என் உயிரே நீயே
            """;

        var result = _service.Process(srt);
        var dto = SrtDtoMapper.ToResponse(result);

        Assert.True(dto.IsValid);
        Assert.Equal(2, dto.Lyrics.Count);

        Assert.Equal(1, dto.Lyrics[0].Index);
        Assert.Equal("00:00:05.000", dto.Lyrics[0].Start);
        Assert.Equal("00:00:09.000", dto.Lyrics[0].End);
        Assert.Equal("என்னை விட்டு போகாதே", dto.Lyrics[0].Text);
        Assert.Equal(4000, dto.Lyrics[0].DurationMs);
        Assert.Equal(0, dto.Lyrics[0].GapToNextMs);

        Assert.Equal(2, dto.Lyrics[1].Index);
        Assert.Equal("00:00:09.000", dto.Lyrics[1].Start);
        Assert.Equal("00:00:13.500", dto.Lyrics[1].End);
        Assert.Equal("என் உயிரே நீயே", dto.Lyrics[1].Text);
        Assert.Equal(4500, dto.Lyrics[1].DurationMs);
        Assert.Null(dto.Lyrics[1].GapToNextMs);

        Assert.Single(dto.Gaps);
        Assert.Equal(0, dto.Gaps[0].GapMs);
        Assert.Empty(dto.Overlaps);
        Assert.Equal(2, dto.Summary.CueCount);
    }

    [Fact]
    public void Process_OverlappingCues_MarksInvalid()
    {
        const string srt = """
            1
            00:00:00,000 --> 00:00:05,000
            First

            2
            00:00:04,000 --> 00:00:08,000
            Overlaps first
            """;

        var result = _service.Process(srt);

        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Overlaps);
        Assert.Contains(result.Issues, i => i.Code == SrtValidationCode.OverlappingCues);
        Assert.Equal(-1000, result.Lyrics[0].GapToNextMs);
    }

    [Fact]
    public void Process_MixedEnglishAndTamil_PreservesUnicode()
    {
        const string srt = """
            1
            00:00:01,000 --> 00:00:03,000
            Hello world

            2
            00:00:03,500 --> 00:00:06,000
            வணக்கம்
            """;

        var result = _service.Process(srt);

        Assert.True(result.IsValid);
        Assert.Equal("Hello world", result.Lyrics[0].Text);
        Assert.Equal("வணக்கம்", result.Lyrics[1].Text);
        Assert.Equal(500, result.Lyrics[0].GapToNextMs);
    }
}
