using Lyricsongs.Domain.Rendering;
using Lyricsongs.Infrastructure.Rendering;

namespace Lyricsongs.Application.Tests.Rendering;

public class FfmpegFilterGraphBuilderTests
{
    private readonly FfmpegFilterGraphBuilder _builder = new();

    [Theory]
    [InlineData(KenBurnsMotion.ZoomIn, "zoompan")]
    [InlineData(KenBurnsMotion.PanLeft, "zoompan")]
    public void BuildKenBurnsFilter_ProducesZoompan(KenBurnsMotion motion, string expected)
    {
        var filter = _builder.BuildKenBurnsFilter(motion, 1920, 1080, 30, 900);
        Assert.Contains(expected, filter);
        Assert.Contains("1920", filter);
        Assert.Contains("1080", filter);
    }

    [Fact]
    public void BuildScalePadFilter_PreservesAspectRatio()
    {
        var filter = _builder.BuildScalePadFilter(1920, 1080, 30);
        Assert.Contains("force_original_aspect_ratio=decrease", filter);
        Assert.Contains("pad=1920:1080", filter);
    }

    [Fact]
    public void BuildAssOverlayFilter_EscapesWindowsPath()
    {
        var filter = _builder.BuildAssOverlayFilter(@"C:\temp\lyrics.ass");
        Assert.StartsWith("ass='", filter);
        Assert.Contains("lyrics.ass", filter);
    }

    [Fact]
    public void BuildXfadeFilterComplex_ChainsMultipleSegments()
    {
        var filter = _builder.BuildXfadeFilterComplex(
            [35.0, 40.0, 35.0],
            [0.5, 0.5],
            "fade");

        Assert.Contains("xfade=transition=fade", filter);
        Assert.Contains("[vout]", filter);
    }

    [Fact]
    public void BuildXfadeFilterComplex_SingleSegment_ReturnsCopy()
    {
        var filter = _builder.BuildXfadeFilterComplex([35.0], [], "fade");
        Assert.Equal("[0:v]copy[vout]", filter);
    }
}
