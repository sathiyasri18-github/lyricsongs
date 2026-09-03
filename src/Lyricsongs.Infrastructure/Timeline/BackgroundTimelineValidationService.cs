using Lyricsongs.Application.Timeline;
using Lyricsongs.Domain.Timeline;

namespace Lyricsongs.Infrastructure.Timeline;

/// <summary>
/// Validates timeline continuity, duration bounds, and transition constraints.
/// </summary>
public sealed class BackgroundTimelineValidationService : IBackgroundTimelineValidationService
{
    private const long GapToleranceMs = 1;

    public TimelineValidationResult Validate(BackgroundTimeline timeline, BackgroundTimelineRequest request)
    {
        ArgumentNullException.ThrowIfNull(timeline);
        ArgumentNullException.ThrowIfNull(request);

        var issues = new List<TimelineValidationIssue>();

        if (timeline.Segments.Count == 0)
        {
            return new TimelineValidationResult { Issues = issues };
        }

        ValidateContinuity(timeline, issues);
        ValidateDurationBounds(timeline, issues);
        ValidateTransitions(timeline, issues);
        ValidateAdjacentDuplicateImages(timeline, request, issues);
        ValidateVideoPlayback(timeline, issues);

        return new TimelineValidationResult { Issues = issues };
    }

    private static void ValidateContinuity(BackgroundTimeline timeline, List<TimelineValidationIssue> issues)
    {
        var segments = timeline.Segments;

        if (segments[0].StartMs != 0)
        {
            issues.Add(new TimelineValidationIssue
            {
                Code = TimelineValidationCode.TimelineGap,
                Message = $"Timeline must start at 00:00:00.000 but starts at {segments[0].Start}.",
                Severity = TimelineValidationSeverity.Error,
                SegmentSequence = segments[0].Sequence
            });
        }

        for (var i = 1; i < segments.Count; i++)
        {
            var previous = segments[i - 1];
            var current = segments[i];
            var expectedStart = previous.EndMs - previous.TransitionOutDurationMs;
            var delta = current.StartMs - expectedStart;

            if (Math.Abs(delta) > GapToleranceMs)
            {
                var code = delta > 0
                    ? TimelineValidationCode.TimelineGap
                    : TimelineValidationCode.TimelineOverflow;

                issues.Add(new TimelineValidationIssue
                {
                    Code = code,
                    Message = delta > 0
                        ? $"Gap of {delta} ms between segment {previous.Sequence} and {current.Sequence}."
                        : $"Overlap of {-delta} ms between segment {previous.Sequence} and {current.Sequence} beyond the configured transition.",
                    Severity = TimelineValidationSeverity.Error,
                    SegmentSequence = current.Sequence,
                    AssetId = current.AssetId
                });
            }
        }
    }

    private static void ValidateDurationBounds(BackgroundTimeline timeline, List<TimelineValidationIssue> issues)
    {
        var last = timeline.Segments[^1];
        var delta = last.EndMs - timeline.AudioDurationMs;

        if (delta > GapToleranceMs)
        {
            issues.Add(new TimelineValidationIssue
            {
                Code = TimelineValidationCode.TimelineOverflow,
                Message = $"Timeline exceeds audio duration by {delta} ms (ends at {last.End}, expected {timeline.AudioDuration}).",
                Severity = TimelineValidationSeverity.Error,
                SegmentSequence = last.Sequence
            });
        }
        else if (delta < -GapToleranceMs)
        {
            issues.Add(new TimelineValidationIssue
            {
                Code = TimelineValidationCode.TimelineUnderrun,
                Message = $"Timeline ends {Math.Abs(delta)} ms before audio duration (ends at {last.End}, expected {timeline.AudioDuration}).",
                Severity = TimelineValidationSeverity.Error,
                SegmentSequence = last.Sequence
            });
        }

        foreach (var segment in timeline.Segments)
        {
            if (segment.SlotDurationMs <= 0)
            {
                issues.Add(new TimelineValidationIssue
                {
                    Code = TimelineValidationCode.InvalidSegmentDuration,
                    Message = $"Segment {segment.Sequence} ({segment.FileName}) has non-positive duration.",
                    Severity = TimelineValidationSeverity.Error,
                    SegmentSequence = segment.Sequence,
                    AssetId = segment.AssetId
                });
            }

            if (segment.EndMs <= segment.StartMs)
            {
                issues.Add(new TimelineValidationIssue
                {
                    Code = TimelineValidationCode.InvalidSegmentDuration,
                    Message = $"Segment {segment.Sequence} end time must be after start time.",
                    Severity = TimelineValidationSeverity.Error,
                    SegmentSequence = segment.Sequence,
                    AssetId = segment.AssetId
                });
            }
        }
    }

    private static void ValidateTransitions(BackgroundTimeline timeline, List<TimelineValidationIssue> issues)
    {
        var segments = timeline.Segments;

        for (var i = 0; i < segments.Count - 1; i++)
        {
            var current = segments[i];
            var next = segments[i + 1];
            var transition = current.TransitionOutDurationMs;

            if (transition > current.SlotDurationMs)
            {
                issues.Add(new TimelineValidationIssue
                {
                    Code = TimelineValidationCode.TransitionExceedsSegment,
                    Message = $"Transition out of segment {current.Sequence} ({transition} ms) exceeds its slot duration ({current.SlotDurationMs} ms).",
                    Severity = TimelineValidationSeverity.Error,
                    SegmentSequence = current.Sequence,
                    AssetId = current.AssetId
                });
            }

            if (transition > next.SlotDurationMs)
            {
                issues.Add(new TimelineValidationIssue
                {
                    Code = TimelineValidationCode.TransitionExceedsSegment,
                    Message = $"Transition into segment {next.Sequence} ({transition} ms) exceeds its slot duration ({next.SlotDurationMs} ms).",
                    Severity = TimelineValidationSeverity.Error,
                    SegmentSequence = next.Sequence,
                    AssetId = next.AssetId
                });
            }
        }
    }

    private static void ValidateAdjacentDuplicateImages(
        BackgroundTimeline timeline,
        BackgroundTimelineRequest request,
        List<TimelineValidationIssue> issues)
    {
        var assetById = request.Assets.ToDictionary(a => a.Id);

        for (var i = 1; i < timeline.Segments.Count; i++)
        {
            var prev = timeline.Segments[i - 1];
            var curr = timeline.Segments[i];

            if (!assetById.TryGetValue(prev.AssetId, out var prevAsset) ||
                !assetById.TryGetValue(curr.AssetId, out var currAsset))
            {
                continue;
            }

            if (prevAsset.Type == BackgroundAssetType.Image &&
                currAsset.Type == BackgroundAssetType.Image &&
                (string.Equals(prevAsset.Id, currAsset.Id, StringComparison.Ordinal) ||
                 string.Equals(prevAsset.FileName, currAsset.FileName, StringComparison.OrdinalIgnoreCase)))
            {
                issues.Add(new TimelineValidationIssue
                {
                    Code = TimelineValidationCode.AdjacentDuplicateImage,
                    Message = $"Image '{currAsset.FileName}' appears consecutively at segments {prev.Sequence} and {curr.Sequence}.",
                    Severity = TimelineValidationSeverity.Warning,
                    SegmentSequence = curr.Sequence,
                    AssetId = curr.AssetId
                });
            }
        }
    }

    private static void ValidateVideoPlayback(BackgroundTimeline timeline, List<TimelineValidationIssue> issues)
    {
        foreach (var segment in timeline.Segments.Where(s => s.AssetType == BackgroundAssetType.Video))
        {
            if (segment.SourceDurationMs is null or <= 0)
            {
                issues.Add(new TimelineValidationIssue
                {
                    Code = TimelineValidationCode.MissingVideoDuration,
                    Message = $"Video segment {segment.Sequence} ({segment.FileName}) is missing source duration metadata.",
                    Severity = TimelineValidationSeverity.Error,
                    SegmentSequence = segment.Sequence,
                    AssetId = segment.AssetId
                });
            }
        }
    }
}
