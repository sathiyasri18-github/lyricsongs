using Lyricsongs.Application.Lyrics;
using Lyricsongs.Domain.Lyrics;
using Lyricsongs.Domain.Subtitles;
using Lyricsongs.Infrastructure.Lyrics;
using Lyricsongs.Infrastructure.Lyrics.Renderers;

namespace Lyricsongs.Application.Tests.Lyrics;

public class LyricAnimationEngineTests
{
    private readonly ILyricAnimationEngine _engine = CreateEngine();

    private static List<LyricCue> SampleCues =>
    [
        new()
        {
            Index = 1,
            Start = "00:00:05.000",
            End = "00:00:09.000",
            Text = "என்னை விட்டு போகாதே",
            StartMs = 5000,
            EndMs = 9000,
            DurationMs = 4000
        },
        new()
        {
            Index = 2,
            Start = "00:00:09.000",
            End = "00:00:13.500",
            Text = "Hello world",
            StartMs = 9000,
            EndMs = 13500,
            DurationMs = 4500
        }
    ];

    [Theory]
    [InlineData(LyricAnimationStyle.Classic, "\\fad")]
    [InlineData(LyricAnimationStyle.Pop, "\\fscx")]
    [InlineData(LyricAnimationStyle.Cinematic, "\\move")]
    [InlineData(LyricAnimationStyle.Minimal, "\\fad")]
    public void Render_AllStyles_ProduceAssWithExpectedTags(LyricAnimationStyle style, string expectedTag)
    {
        var plan = _engine.Render(SampleCues, Config(style));

        Assert.Equal(style, plan.Style);
        Assert.Contains("[Script Info]", plan.AssContent);
        Assert.Contains("Noto Sans Tamil", plan.AssContent);
        Assert.Contains(expectedTag, plan.AssContent);
        Assert.Contains("ass='lyrics.ass'", plan.Ffmpeg.SubtitleFilter);
    }

    [Fact]
    public void Render_Karaoke_ContainsKaraokeFillTags()
    {
        var plan = _engine.Render(SampleCues, Config(LyricAnimationStyle.Karaoke));

        Assert.Contains("\\kf", plan.AssContent);
        Assert.Contains("என்னை", plan.AssContent);
        Assert.All(plan.StyledCues, c => Assert.Contains("\\kf", c.AssDialogueText));
    }

    [Fact]
    public void Render_TamilUnicode_PreservedInAss()
    {
        var plan = _engine.Render(SampleCues, Config(LyricAnimationStyle.Classic));

        Assert.Contains("என்னை விட்டு போகாதே", plan.AssContent);
    }

    [Fact]
    public void Render_Multiline_FormatsWithAssNewline()
    {
        var cues = new List<LyricCue>
        {
            new()
            {
                Index = 1,
                Start = "00:00:01.000",
                End = "00:00:04.000",
                Text = "Line one\nLine two",
                StartMs = 1000,
                EndMs = 4000,
                DurationMs = 3000
            }
        };

        var plan = _engine.Render(cues, Config(LyricAnimationStyle.Classic));

        Assert.Contains("Line one\\NLine two", plan.AssContent);
    }

    [Fact]
    public void Render_UsesConfiguration_NotHardcodedStyle()
    {
        var classic = _engine.Render(SampleCues, Config(LyricAnimationStyle.Classic));
        var pop = _engine.Render(SampleCues, Config(LyricAnimationStyle.Pop));

        Assert.NotEqual(classic.AssContent, pop.AssContent);
        Assert.Contains("\\fscx", pop.AssContent);
        Assert.DoesNotContain("\\fscx", classic.AssContent);
    }

    [Fact]
    public void ConfigLoader_ParsesJsonExample()
    {
        const string json = """
            {
              "style": "karaoke",
              "font": "Noto Sans Tamil",
              "fontSize": 54,
              "position": "bottom",
              "textColor": "#FFFFFF",
              "highlightColor": "#FFD700",
              "outline": true,
              "shadow": true
            }
            """;

        var loader = new LyricStyleConfigurationLoader();
        var config = loader.Load(json);

        Assert.Equal(LyricAnimationStyle.Karaoke, config.Style);
        Assert.Equal("Noto Sans Tamil", config.Font);
        Assert.Equal(54, config.FontSize);
        Assert.Equal(LyricPosition.Bottom, config.Position);
        Assert.Equal("#FFD700", config.HighlightColor);
    }

    private static LyricStyleConfiguration Config(LyricAnimationStyle style) => new()
    {
        Style = style,
        Font = "Noto Sans Tamil",
        FontSize = 54,
        Position = LyricPosition.Bottom,
        Alignment = LyricTextAlignment.Center,
        TextColor = "#FFFFFF",
        HighlightColor = "#FFD700",
        Outline = true,
        Shadow = true
    };

    private static LyricAnimationEngine CreateEngine()
    {
        return new LyricAnimationEngine(
            new LyricStyleRendererFactory(LyricStyleRendererRegistration.CreateAllRenderers()));
    }
}
