using Lyricsongs.Domain.Timeline;
using Lyricsongs.Infrastructure.Timeline;

namespace Lyricsongs.Application.Tests.Timeline;

public class BackgroundTimelineValidationServiceTests
{
    private readonly BackgroundTimelineValidationService _validator = new();
    private readonly BackgroundTimelineService _builder = new(new BackgroundTimelineValidationService());

    [Fact]
    public void Validate_ValidTimeline_HasNoErrors()
    {
        var request = TimelineTestData.CreateExampleRequest();
        var build = _builder.Build(request);
        var result = _validator.Validate(build.Timeline, request);

        Assert.True(result.IsValid);
        Assert.Empty(result.Issues.Where(i => i.Severity == TimelineValidationSeverity.Error));
    }

    [Fact]
    public void Validate_TimelineGap_DetectsError()
    {
        var request = TimelineTestData.CreateExampleRequest();
        var build = _builder.Build(request);
        var segments = build.Timeline.Segments.ToList();
        var original = segments[1];
        segments[1] = CloneSegment(original, startMs: original.StartMs + 500);

        var timeline = CloneTimeline(build.Timeline, segments);
        var result = _validator.Validate(timeline, request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Code == TimelineValidationCode.TimelineGap);
    }

    [Fact]
    public void Validate_TimelineOverflow_DetectsError()
    {
        var request = TimelineTestData.CreateExampleRequest();
        var build = _builder.Build(request);
        var segments = build.Timeline.Segments.ToList();
        var last = segments[^1];
        segments[^1] = CloneSegment(last, endMs: last.EndMs + 1000);

        var timeline = CloneTimeline(build.Timeline, segments);
        var result = _validator.Validate(timeline, request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Code == TimelineValidationCode.TimelineOverflow);
    }

    [Fact]
    public void Validate_TransitionExceedsSegment_DetectsError()
    {
        var request = TimelineTestData.CreateExampleRequest();
        var build = _builder.Build(request);
        var segments = build.Timeline.Segments.ToList();
        var first = segments[0];
        segments[0] = CloneSegment(first, transitionOutDurationMs: first.SlotDurationMs + 1);

        var timeline = CloneTimeline(build.Timeline, segments);
        var result = _validator.Validate(timeline, request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Code == TimelineValidationCode.TransitionExceedsSegment);
    }

    private static BackgroundTimeline CloneTimeline(BackgroundTimeline source, List<TimelineSegment> segments) => new()
    {
        AudioDurationMs = source.AudioDurationMs,
        AudioDuration = source.AudioDuration,
        AvailableVideoDurationMs = source.AvailableVideoDurationMs,
        TransitionDurationMs = source.TransitionDurationMs,
        OrderingMode = source.OrderingMode,
        OrderingSeed = source.OrderingSeed,
        OrderedAssetIds = source.OrderedAssetIds,
        Segments = segments,
        Summary = source.Summary
    };

    private static TimelineSegment CloneSegment(
        TimelineSegment source,
        long? startMs = null,
        long? endMs = null,
        long? transitionOutDurationMs = null) => new()
    {
        Sequence = source.Sequence,
        AssetId = source.AssetId,
        FileName = source.FileName,
        AssetType = source.AssetType,
        StartMs = startMs ?? source.StartMs,
        EndMs = endMs ?? source.EndMs,
        Start = source.Start,
        End = source.End,
        SlotDurationMs = source.SlotDurationMs,
        TransitionOutDurationMs = transitionOutDurationMs ?? source.TransitionOutDurationMs,
        PlaybackMode = source.PlaybackMode,
        LoopCount = source.LoopCount,
        TrimStartMs = source.TrimStartMs,
        SourcePlayDurationMs = source.SourcePlayDurationMs,
        SourceDurationMs = source.SourceDurationMs
    };
}
