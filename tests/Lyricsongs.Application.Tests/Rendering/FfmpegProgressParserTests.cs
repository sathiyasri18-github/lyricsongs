using Lyricsongs.Infrastructure.Rendering;

namespace Lyricsongs.Application.Tests.Rendering;

public class FfmpegProgressParserTests
{
    private readonly FfmpegProgressParser _parser = new();

    [Fact]
    public void ParseProgressLine_OutTimeMs_ReturnsDuration()
    {
        var update = _parser.ParseProgressLine("out_time_ms=5000000");
        Assert.NotNull(update);
        Assert.Equal(5000, update!.ProcessedDurationMs);
    }

    [Fact]
    public void ParseStatsLine_TimePattern_ReturnsDuration()
    {
        var update = _parser.ParseStatsLine("frame=  120 fps= 30 q=28.0 size=    1024kB time=00:00:04.00 bitrate= 123.4kbits/s speed=1.0x");
        Assert.NotNull(update);
        Assert.Equal(4000, update!.ProcessedDurationMs);
    }

    [Fact]
    public void ParseStatsLine_InvalidLine_ReturnsNull()
    {
        Assert.Null(_parser.ParseStatsLine("configuration: --enable-gpl"));
    }
}
