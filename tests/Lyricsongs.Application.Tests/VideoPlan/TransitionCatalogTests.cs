using Lyricsongs.Domain.VideoPlan;

namespace Lyricsongs.Application.Tests.VideoPlan;

public class TransitionCatalogTests
{
    [Theory]
    [InlineData(TransitionType.WipeLeft, "wipeleft")]
    [InlineData(TransitionType.SlideRight, "slideright")]
    [InlineData(TransitionType.FadeBlack, "fadeblack")]
    [InlineData(TransitionType.DiagTopLeft, "diagtl")]
    [InlineData(TransitionType.HorizontalOpen, "horzopen")]
    public void ToFfmpegName_MapsKnownTransitions(TransitionType type, string expected)
    {
        Assert.Equal(expected, TransitionCatalog.ToFfmpegName(type));
    }

    [Theory]
    [InlineData("wipeLeft", TransitionType.WipeLeft)]
    [InlineData("WipeLeft", TransitionType.WipeLeft)]
    [InlineData("slideUp", TransitionType.SlideUp)]
    [InlineData("none", TransitionType.None)]
    public void TryParse_AcceptsCamelCase(string value, TransitionType expected)
    {
        Assert.True(TransitionCatalog.TryParse(value, out var type));
        Assert.Equal(expected, type);
    }
}
