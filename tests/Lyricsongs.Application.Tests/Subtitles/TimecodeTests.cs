using Lyricsongs.Domain.Subtitles;

namespace Lyricsongs.Application.Tests.Subtitles;

public class TimecodeTests
{
    [Theory]
    [InlineData("00:00:05,000", 5000)]
    [InlineData("00:00:05.000", 5000)]
    [InlineData("01:02:03,456", 3723456)]
    [InlineData("0:00:01,5", 1500)]
    public void TryParse_ValidFormats_Succeeds(string input, long expectedMs)
    {
        var ok = Timecode.TryParse(input, out var tc);
        Assert.True(ok);
        Assert.Equal(expectedMs, tc.TotalMilliseconds);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("00:60:00,000")]
    [InlineData("00:00:60,000")]
    [InlineData("00:00:00,1000")]
    public void TryParse_InvalidFormats_Fails(string input)
    {
        Assert.False(Timecode.TryParse(input, out _));
    }

    [Fact]
    public void ToDisplayString_UsesDotSeparator()
    {
        var tc = Timecode.FromComponents(0, 0, 9, 500);
        Assert.Equal("00:00:09.500", tc.ToDisplayString());
    }

    [Fact]
    public void ToSrtString_UsesCommaSeparator()
    {
        var tc = Timecode.FromComponents(0, 0, 9, 500);
        Assert.Equal("00:00:09,500", tc.ToSrtString());
    }
}
