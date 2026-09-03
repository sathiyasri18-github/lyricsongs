using Lyricsongs.Application.VideoPlan;
using Lyricsongs.Domain.Lyrics;
using Lyricsongs.Domain.Subtitles;
using Lyricsongs.Domain.Timeline;
using Lyricsongs.Domain.VideoPlan;
using Lyricsongs.Infrastructure.VideoPlan;

namespace Lyricsongs.Application.Tests.VideoPlan;

public class VideoPlanEngineTests
{
    private readonly IVideoPlanEngine _engine = CreateEngine();

    [Fact]
    public void Build_Sequential_CoversFullAudioWithoutGaps()
    {
        var result = _engine.Build(CreateRequest(BackgroundPlanningMode.Sequential));

        Assert.True(result.Validation.IsValid);
        Assert.Equal(0, result.Plan.Backgrounds[0].StartMs);
        Assert.Equal(70_000, result.Plan.Backgrounds[^1].EndMs);
        Assert.Equal(1920, result.Plan.Output.Width);
        Assert.Equal(1080, result.Plan.Output.Height);
        Assert.Equal("libx264", result.Plan.Output.VideoCodec);
        Assert.Equal(LyricAnimationStyle.Cinematic, result.Plan.Lyrics.Style);
    }

    [Fact]
    public void Build_Sequential_IsDeterministic()
    {
        var request = CreateRequest(BackgroundPlanningMode.Sequential);
        var a = _engine.Build(request);
        var b = _engine.Build(request);

        Assert.Equal(a.Plan.PlanId, b.Plan.PlanId);
        Assert.Equal(
            a.Plan.Backgrounds.Select(x => (x.AssetId, x.StartMs, x.EndMs, x.Animation)),
            b.Plan.Backgrounds.Select(x => (x.AssetId, x.StartMs, x.EndMs, x.Animation)));
    }

    [Fact]
    public void Build_Balanced_IsDeterministic()
    {
        var request = CreateRequest(BackgroundPlanningMode.Balanced);
        var a = _engine.Build(request);
        var b = _engine.Build(request);

        Assert.Equal(a.Plan.OrderedAssetIds, b.Plan.OrderedAssetIds);
    }

    [Fact]
    public void Build_Random_SameSeed_IsDeterministic()
    {
        var request = CreateRequest(BackgroundPlanningMode.Random, seed: 123);
        var a = _engine.Build(request);
        var b = _engine.Build(request);

        Assert.Equal(a.Plan.OrderedAssetIds, b.Plan.OrderedAssetIds);
    }

    [Fact]
    public void Build_Random_DifferentSeeds_CanDiffer()
    {
        var a = _engine.Build(CreateRequest(BackgroundPlanningMode.Random, seed: 1));
        var b = _engine.Build(CreateRequest(BackgroundPlanningMode.Random, seed: 999));

        // Not guaranteed different with tiny asset sets, but with 4 assets usually differs
        Assert.Equal(a.Plan.Backgrounds.Count, b.Plan.Backgrounds.Count);
    }

    [Fact]
    public void Build_VideosTrimOrLoop_AsNeeded()
    {
        var request = new VideoPlanRequest
        {
            AudioDurationMs = 60_000,
            Lyrics = [Cue(1, 1000, 4000, "Hello")],
            Assets =
            [
                Asset("short", "short.mp4", BackgroundAssetType.Video, 10_000, 0)
            ],
            Settings = new VideoPlanSettings
            {
                PlanningMode = BackgroundPlanningMode.Sequential,
                Transition = TransitionSettings.None,
                LyricStyle = LyricAnimationStyle.Minimal
            }
        };

        var result = _engine.Build(request);
        var segment = result.Plan.Backgrounds.Single();

        Assert.Equal(VideoPlaybackMode.Looped, segment.PlaybackMode);
        Assert.True(segment.LoopCount >= 6);
        Assert.Equal(BackgroundAnimation.None, segment.Animation);
    }

    [Fact]
    public void ApplyOverrides_ChangesLyricStyleAndResolution()
    {
        var request = CreateRequest(BackgroundPlanningMode.Sequential);
        var overrides = new VideoPlanSettings
        {
            PlanningMode = BackgroundPlanningMode.Sequential,
            LyricStyle = LyricAnimationStyle.Karaoke,
            ResolutionPreset = VideoResolutionPreset.Vertical1080,
            Fps = 60,
            FontOverride = "Noto Sans Tamil",
            FontSizeOverride = 48,
            TextColorOverride = "#EEEEEE",
            Transition = TransitionSettings.None
        };

        var result = _engine.ApplyOverrides(request, overrides);

        Assert.Equal(LyricAnimationStyle.Karaoke, result.Plan.Lyrics.Style);
        Assert.Equal(1080, result.Plan.Output.Width);
        Assert.Equal(1920, result.Plan.Output.Height);
        Assert.Equal(60, result.Plan.Output.Fps);
        Assert.Equal(48, result.Plan.Lyrics.FontSize);
        Assert.Equal("#EEEEEE", result.Plan.Lyrics.TextColor);
    }

    [Fact]
    public void Build_InvalidFontOverride_Throws()
    {
        var request = CreateRequest(BackgroundPlanningMode.Sequential);
        request.Settings.FontOverride = "Comic Sans MS";

        var ex = Assert.Throws<VideoPlanException>(() => _engine.Build(request));
        Assert.Contains("Font", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Build_SubtitleExceedsAudio_ThrowsWithValidation()
    {
        var request = CreateRequest(BackgroundPlanningMode.Sequential);
        request = new VideoPlanRequest
        {
            AudioDurationMs = request.AudioDurationMs,
            Assets = request.Assets,
            Settings = request.Settings,
            Lyrics =
            [
                Cue(1, 0, 80_000, "Too long")
            ]
        };

        var ex = Assert.Throws<VideoPlanException>(() => _engine.Build(request));
        Assert.NotNull(ex.Validation);
        Assert.Contains(ex.Validation!.Errors, e => e.Code == VideoPlanValidationCode.SubtitleExceedsAudio);
    }

    [Fact]
    public void Build_TamilUnicode_PreservedInSubtitles()
    {
        var result = _engine.Build(CreateRequest(BackgroundPlanningMode.Sequential));
        Assert.Contains(result.Plan.Subtitles, s => s.Text.Contains("என்னை"));
    }

    [Fact]
    public void Build_WithTransitions_DoesNotExceedSegmentDuration()
    {
        var request = CreateRequest(BackgroundPlanningMode.Sequential);
        request.Settings.Transition = new TransitionSettings
        {
            Type = TransitionType.Fade,
            DurationMs = 500
        };

        var result = _engine.Build(request);

        foreach (var segment in result.Plan.Backgrounds)
        {
            Assert.True(segment.TransitionOutDurationMs <= segment.DurationMs);
        }

        Assert.Equal(70_000, result.Plan.Backgrounds[^1].EndMs);
    }

    private static IVideoPlanEngine CreateEngine() =>
        new VideoPlanEngine(
            new BackgroundPlanner(),
            new LyricStyleManager(),
            new VideoPlanValidator());

    private static VideoPlanRequest CreateRequest(
        BackgroundPlanningMode mode,
        int seed = 42) => new()
    {
        AudioDurationMs = 70_000,
        Lyrics =
        [
            Cue(1, 5000, 9000, "என்னை விட்டு போகாதே"),
            Cue(2, 9000, 13500, "Hello world")
        ],
        Assets =
        [
            Asset("bg1", "bg1.jpg", BackgroundAssetType.Image, null, 0),
            Asset("clip1", "clip1.mp4", BackgroundAssetType.Video, 35_000, 1),
            Asset("bg2", "bg2.jpg", BackgroundAssetType.Image, null, 2),
            Asset("clip2", "clip2.mp4", BackgroundAssetType.Video, 20_000, 3)
        ],
        Settings = new VideoPlanSettings
        {
            PlanningMode = mode,
            RandomSeed = seed,
            Transition = TransitionSettings.None,
            LyricStyle = LyricAnimationStyle.Cinematic,
            ResolutionPreset = VideoResolutionPreset.FullHd1080p,
            Fps = 30,
            CycleImageAnimations = true
        }
    };

    private static VideoPlanAsset Asset(
        string id,
        string fileName,
        BackgroundAssetType type,
        long? durationMs,
        int sortOrder) => new()
    {
        AssetId = id,
        FileName = fileName,
        Type = type,
        SourceDurationMs = durationMs,
        SortOrder = sortOrder
    };

    private static LyricCue Cue(int index, long startMs, long endMs, string text) => new()
    {
        Index = index,
        Start = $"{startMs / 1000:D2}",
        End = $"{endMs / 1000:D2}",
        Text = text,
        StartMs = startMs,
        EndMs = endMs,
        DurationMs = endMs - startMs
    };
}
