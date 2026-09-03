using Lyricsongs.Domain.Lyrics;
using Lyricsongs.Infrastructure.Lyrics;
using Lyricsongs.Infrastructure.VideoPlan;

namespace Lyricsongs.Application.Tests.Lyrics;

public class LyricStyleRendererFactoryTests
{
    private readonly LyricStyleRendererFactory _factory =
        new(LyricStyleRendererRegistration.CreateAllRenderers());

    [Fact]
    public void SupportedStyles_ContainsAllCatalogStyles()
    {
        Assert.Equal(LyricStyleCatalog.AllStyles.Count, _factory.SupportedStyles.Count);
        Assert.Contains(LyricAnimationStyle.Karaoke, _factory.SupportedStyles);
        Assert.Contains(LyricAnimationStyle.Neon, _factory.SupportedStyles);
        Assert.Contains(LyricAnimationStyle.Elegant, _factory.SupportedStyles);
    }

    [Fact]
    public void GetRenderer_ReturnsRegisteredRenderer()
    {
        var renderer = _factory.GetRenderer(LyricAnimationStyle.Pop);
        Assert.Equal(LyricAnimationStyle.Pop, renderer.Style);
    }

    [Fact]
    public void GetRenderer_UnknownStyle_Throws()
    {
        Assert.Throws<NotSupportedException>(() =>
            _factory.GetRenderer((LyricAnimationStyle)999));
    }
}
