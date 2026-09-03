using Lyricsongs.Application.VideoPlan.Models;
using Lyricsongs.Domain.Lyrics;
using Lyricsongs.Domain.Subtitles;
using Lyricsongs.Domain.Timeline;
using Lyricsongs.Domain.VideoPlan;
using Lyricsongs.Infrastructure.VideoPlan;

namespace Lyricsongs.Application.Tests.VideoPlan;

public class VideoPlanDtoMapperTests
{
    [Fact]
    public void ToJson_MatchesExpectedShape()
    {
        var engine = new VideoPlanEngine(
            new BackgroundPlanner(),
            new LyricStyleManager(),
            new VideoPlanValidator());

        var result = engine.Build(new VideoPlanRequest
        {
            AudioDurationMs = 70_000,
            Lyrics =
            [
                new LyricCue
                {
                    Index = 1,
                    Start = "00:00:05.000",
                    End = "00:00:09.000",
                    Text = "என்னை விட்டு போகாதே",
                    StartMs = 5000,
                    EndMs = 9000,
                    DurationMs = 4000
                }
            ],
            Assets =
            [
                new VideoPlanAsset { AssetId = "bg1", FileName = "bg1.jpg", Type = BackgroundAssetType.Image, SortOrder = 0 },
                new VideoPlanAsset { AssetId = "clip1", FileName = "clip1.mp4", Type = BackgroundAssetType.Video, SourceDurationMs = 35_000, SortOrder = 1 }
            ],
            Settings = new VideoPlanSettings
            {
                PlanningMode = BackgroundPlanningMode.Sequential,
                Transition = TransitionSettings.None,
                LyricStyle = LyricAnimationStyle.Cinematic,
                CycleImageAnimations = false,
                DefaultImageAnimation = BackgroundAnimation.ZoomIn
            }
        });

        var dto = VideoPlanDtoMapper.ToDto(result.Plan);
        var json = VideoPlanDtoMapper.ToJson(result.Plan);

        Assert.Equal(1920, dto.Output.Width);
        Assert.Equal(1080, dto.Output.Height);
        Assert.Equal(30, dto.Output.Fps);
        Assert.Equal("libx264", dto.Output.VideoCodec);
        Assert.Equal("cinematic", dto.Lyrics.Style);
        Assert.Equal("Noto Sans Tamil", dto.Lyrics.Font);
        Assert.Contains("\"backgrounds\"", json);
        Assert.Contains("bg1", json);
        Assert.Equal("என்னை விட்டு போகாதே", dto.Subtitles[0].Text);
        Assert.Contains("\\u0B8E", json); // Tamil letter எ escaped in System.Text.Json
    }
}
