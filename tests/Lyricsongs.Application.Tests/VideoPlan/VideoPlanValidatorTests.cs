using Lyricsongs.Application.VideoPlan;
using Lyricsongs.Domain.Lyrics;
using Lyricsongs.Domain.Subtitles;
using Lyricsongs.Domain.Timeline;
using Lyricsongs.Domain.VideoPlan;
using Lyricsongs.Infrastructure.VideoPlan;

namespace Lyricsongs.Application.Tests.VideoPlan;

public class VideoPlanValidatorTests
{
    private readonly VideoPlanValidator _validator = new();
    private readonly VideoPlanEngine _engine = new(
        new BackgroundPlanner(),
        new LyricStyleManager(),
        new VideoPlanValidator());

    [Fact]
    public void Validate_ValidPlan_Passes()
    {
        var request = CreateValidRequest();
        var build = _engine.Build(request);
        Assert.True(build.Validation.IsValid);
    }

    [Fact]
    public void Validate_UnknownAsset_Fails()
    {
        var request = CreateValidRequest();
        var build = _engine.Build(request);

        var badSegments = build.Plan.Backgrounds.ToList();
        badSegments[0] = new BackgroundSegment
        {
            AssetId = "missing",
            Type = BackgroundAssetType.Image,
            StartMs = badSegments[0].StartMs,
            EndMs = badSegments[0].EndMs,
            Animation = BackgroundAnimation.ZoomIn
        };

        var badPlan = ClonePlan(build.Plan, badSegments);
        var result = _validator.Validate(badPlan, request.Assets, request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == VideoPlanValidationCode.UnknownAssetId);
    }

    [Fact]
    public void Validate_InvalidResolution_Fails()
    {
        var request = CreateValidRequest();
        var build = _engine.Build(request);
        var badOutput = new VideoOutputSettings
        {
            Width = 800,
            Height = 600,
            Fps = 30,
            VideoCodec = "libx264",
            AudioCodec = "aac",
            Format = "mp4"
        };

        var badPlan = ClonePlan(build.Plan, build.Plan.Backgrounds.ToList(), badOutput);
        var result = _validator.Validate(badPlan, request.Assets, request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == VideoPlanValidationCode.InvalidResolution);
    }

    private static VideoPlanRequest CreateValidRequest() => new()
    {
        AudioDurationMs = 40_000,
        Lyrics = [new LyricCue
        {
            Index = 1,
            Start = "00:00:01.000",
            End = "00:00:03.000",
            Text = "Test",
            StartMs = 1000,
            EndMs = 3000,
            DurationMs = 2000
        }],
        Assets =
        [
            new VideoPlanAsset
            {
                AssetId = "bg1",
                FileName = "bg1.jpg",
                Type = BackgroundAssetType.Image,
                SortOrder = 0
            },
            new VideoPlanAsset
            {
                AssetId = "bg2",
                FileName = "bg2.jpg",
                Type = BackgroundAssetType.Image,
                SortOrder = 1
            }
        ],
        Settings = new VideoPlanSettings
        {
            PlanningMode = BackgroundPlanningMode.Sequential,
            Transition = TransitionSettings.None,
            LyricStyle = LyricAnimationStyle.Minimal
        }
    };

    private static Lyricsongs.Domain.VideoPlan.VideoPlan ClonePlan(
        Lyricsongs.Domain.VideoPlan.VideoPlan source,
        List<BackgroundSegment> backgrounds,
        VideoOutputSettings? output = null) => new()
    {
        PlanId = source.PlanId,
        AudioDurationMs = source.AudioDurationMs,
        Output = output ?? source.Output,
        Backgrounds = backgrounds,
        Lyrics = source.Lyrics,
        Transition = source.Transition,
        Subtitles = source.Subtitles,
        OrderedAssetIds = source.OrderedAssetIds,
        PlanningMode = source.PlanningMode,
        RandomSeed = source.RandomSeed
    };
}
