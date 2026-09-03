using Lyricsongs.Application.Timeline;
using Lyricsongs.Domain.Timeline;
using Lyricsongs.Infrastructure.Timeline;

namespace Lyricsongs.Application.Tests.Timeline;

public class BackgroundTimelineServiceTests
{
    private readonly BackgroundTimelineService _service = new(new BackgroundTimelineValidationService());

    [Fact]
    public void Build_ExampleScenario_CoversFullAudioDuration()
    {
        var result = _service.Build(TimelineTestData.CreateExampleRequest());

        Assert.True(result.Validation.IsValid);
        Assert.Equal(245_000, result.Timeline.Summary.TimelineEndMs);
        Assert.Equal(6, result.Timeline.Segments.Count);
        Assert.Equal(75_000, result.Timeline.AvailableVideoDurationMs);
    }

    [Fact]
    public void Build_ExampleScenario_VideosUseSourceDuration_ImagesShareRemainder()
    {
        var result = _service.Build(TimelineTestData.CreateExampleRequest());

        var clip1 = result.Timeline.Segments.Single(s => s.AssetId == "clip1");
        var clip2 = result.Timeline.Segments.Single(s => s.AssetId == "clip2");
        var images = result.Timeline.Segments.Where(s => s.AssetType == BackgroundAssetType.Image).ToList();

        Assert.Equal(35_000, clip1.SlotDurationMs);
        Assert.Equal(40_000, clip2.SlotDurationMs);
        Assert.Equal(VideoPlaybackMode.Normal, clip1.PlaybackMode);
        Assert.Equal(VideoPlaybackMode.Normal, clip2.PlaybackMode);
        Assert.All(images, img => Assert.Equal(42_500, img.SlotDurationMs));
    }

    [Fact]
    public void Build_ExampleScenario_ManualOrder_MatchesSortOrder()
    {
        var result = _service.Build(TimelineTestData.CreateExampleRequest());

        Assert.Equal(
            ["bg1", "clip1", "bg2", "bg3", "clip2", "bg4"],
            result.Timeline.OrderedAssetIds);
    }

    [Fact]
    public void Build_NoGaps_SegmentsAreContiguous()
    {
        var result = _service.Build(TimelineTestData.CreateExampleRequest(transitionMs: 500));

        for (var i = 1; i < result.Timeline.Segments.Count; i++)
        {
            var prev = result.Timeline.Segments[i - 1];
            var curr = result.Timeline.Segments[i];
            Assert.Equal(prev.EndMs - prev.TransitionOutDurationMs, curr.StartMs);
        }

        Assert.Equal(0, result.Timeline.Segments[0].StartMs);
        Assert.Equal(245_000, result.Timeline.Segments[^1].EndMs);
    }

    [Fact]
    public void Build_WithTransitions_TransitionDoesNotExceedSegmentDuration()
    {
        var result = _service.Build(TimelineTestData.CreateExampleRequest(transitionMs: 500));

        foreach (var segment in result.Timeline.Segments)
        {
            Assert.True(segment.TransitionOutDurationMs <= segment.SlotDurationMs);
        }
    }

    [Fact]
    public void Build_VideoShorterThanSlot_LoopsVideo()
    {
        var request = new BackgroundTimelineRequest
        {
            AudioDurationMs = 60_000,
            TransitionDurationMs = 0,
            Assets =
            [
                TimelineTestData.Asset("clip", "clip.mp4", BackgroundAssetType.Video, 10_000)
            ]
        };

        var result = _service.Build(request);
        var segment = result.Timeline.Segments.Single();

        Assert.Equal(VideoPlaybackMode.Looped, segment.PlaybackMode);
        Assert.Equal(6, segment.LoopCount);
        Assert.Equal(60_000, segment.SlotDurationMs);
    }

    [Fact]
    public void Build_VideoLongerThanSlot_TrimsVideo()
    {
        var request = new BackgroundTimelineRequest
        {
            AudioDurationMs = 20_000,
            TransitionDurationMs = 0,
            Assets =
            [
                TimelineTestData.Asset("clip", "clip.mp4", BackgroundAssetType.Video, 45_000)
            ]
        };

        var result = _service.Build(request);
        var segment = result.Timeline.Segments.Single();

        Assert.Equal(VideoPlaybackMode.Trimmed, segment.PlaybackMode);
        Assert.Equal(20_000, segment.SourcePlayDurationMs);
        Assert.Equal(1, segment.LoopCount);
    }

    [Fact]
    public void Build_AutomaticOrdering_IsDeterministicForSameSeed()
    {
        var request = TimelineTestData.CreateExampleRequest(ordering: AssetOrderingMode.Automatic);

        var first = _service.Build(request);
        var second = _service.Build(request);

        Assert.Equal(first.Timeline.OrderedAssetIds, second.Timeline.OrderedAssetIds);
        Assert.NotEqual(
            TimelineTestData.CreateExampleRequest().Assets.Select(a => a.Id).ToList(),
            first.Timeline.OrderedAssetIds);
    }

    [Fact]
    public void Build_NoAssets_ReturnsError()
    {
        var result = _service.Build(new BackgroundTimelineRequest
        {
            AudioDurationMs = 60_000,
            Assets = []
        });

        Assert.False(result.Validation.IsValid);
        Assert.Contains(result.Validation.Issues, i => i.Code == TimelineValidationCode.NoAssets);
    }

    [Fact]
    public void Build_MissingVideoDuration_ReturnsError()
    {
        var result = _service.Build(new BackgroundTimelineRequest
        {
            AudioDurationMs = 60_000,
            Assets =
            [
                TimelineTestData.Asset("clip", "clip.mp4", BackgroundAssetType.Video, sourceDurationMs: null)
            ]
        });

        Assert.False(result.Validation.IsValid);
        Assert.Contains(result.Validation.Issues, i => i.Code == TimelineValidationCode.MissingVideoDuration);
    }

    [Fact]
    public void Build_ImagesOnly_DistributesEqually()
    {
        var result = _service.Build(new BackgroundTimelineRequest
        {
            AudioDurationMs = 120_000,
            TransitionDurationMs = 0,
            Assets =
            [
                TimelineTestData.Asset("bg1", "bg1.jpg", BackgroundAssetType.Image, sortOrder: 0),
                TimelineTestData.Asset("bg2", "bg2.jpg", BackgroundAssetType.Image, sortOrder: 1),
                TimelineTestData.Asset("bg3", "bg3.jpg", BackgroundAssetType.Image, sortOrder: 2)
            ]
        });

        Assert.True(result.Validation.IsValid);
        Assert.All(result.Timeline.Segments, s => Assert.Equal(40_000, s.SlotDurationMs));
    }

    [Fact]
    public void Build_FormatsTimecodes_WithDotSeparator()
    {
        var result = _service.Build(TimelineTestData.CreateExampleRequest());
        var first = result.Timeline.Segments[0];

        Assert.Equal("00:00:00.000", first.Start);
        Assert.Contains('.', first.End);
        Assert.DoesNotContain(',', first.End);
    }

    [Fact]
    public void Build_AvoidsAdjacentDuplicateImages()
    {
        var result = _service.Build(new BackgroundTimelineRequest
        {
            AudioDurationMs = 90_000,
            TransitionDurationMs = 0,
            OrderingMode = AssetOrderingMode.Manual,
            Assets =
            [
                TimelineTestData.Asset("bg1a", "bg1.jpg", BackgroundAssetType.Image, sortOrder: 0),
                TimelineTestData.Asset("bg1b", "bg1.jpg", BackgroundAssetType.Image, sortOrder: 1),
                TimelineTestData.Asset("bg2", "bg2.jpg", BackgroundAssetType.Image, sortOrder: 2)
            ]
        });

        Assert.True(result.Validation.IsValid);
        Assert.NotEqual(result.Timeline.Segments[0].FileName, result.Timeline.Segments[1].FileName);
    }
}
