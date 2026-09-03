using Lyricsongs.Domain.Lyrics;
using Lyricsongs.Infrastructure.VideoPlan;

namespace Lyricsongs.Application.Tests.VideoPlan;

public class LyricStyleCatalogTests
{
    [Fact]
    public void Presets_ContainsAtLeastTwentyStyles()
    {
        Assert.True(LyricStyleCatalog.Presets.Count >= 20);
    }

    [Fact]
    public void AllStyles_HavePresets()
    {
        foreach (var style in LyricStyleCatalog.AllStyles)
        {
            Assert.True(LyricStyleCatalog.Presets.ContainsKey(style), $"Missing preset for {style}");
        }
    }

    [Theory]
    [InlineData(LyricAnimationStyle.Neon)]
    [InlineData(LyricAnimationStyle.SlideUp)]
    [InlineData(LyricAnimationStyle.Elegant)]
    public void Presets_HaveValidFontSizes(LyricAnimationStyle style)
    {
        var preset = LyricStyleCatalog.Presets[style];
        Assert.InRange(preset.FontSize, 12, 200);
        Assert.False(string.IsNullOrWhiteSpace(preset.Font));
    }
}
