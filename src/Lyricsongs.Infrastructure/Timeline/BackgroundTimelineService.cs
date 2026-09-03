using Lyricsongs.Application.Timeline;
using Lyricsongs.Domain.Subtitles;
using Lyricsongs.Domain.Timeline;

namespace Lyricsongs.Infrastructure.Timeline;

/// <summary>
/// Builds a gap-free background timeline that spans the full audio duration.
/// Videos are trimmed or looped; images receive flexible slot durations.
/// </summary>
public sealed class BackgroundTimelineService : IBackgroundTimelineService
{
    private readonly IBackgroundTimelineValidationService _validationService;

    public BackgroundTimelineService(IBackgroundTimelineValidationService validationService)
    {
        _validationService = validationService;
    }

    public BackgroundTimelineBuildResult Build(BackgroundTimelineRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var preflightIssues = PreflightValidate(request);
        if (preflightIssues.Any(i => i.Severity == TimelineValidationSeverity.Error))
        {
            var emptyTimeline = CreateEmptyTimeline(request);
            return new BackgroundTimelineBuildResult
            {
                Timeline = emptyTimeline,
                Validation = new TimelineValidationResult { Issues = preflightIssues }
            };
        }

        var orderedAssets = OrderAssets(request.Assets, request.OrderingMode, request.OrderingSeed);
        var n = orderedAssets.Count;
        var transitionCount = Math.Max(0, n - 1);

        var effectiveTransitionMs = ComputeEffectiveTransitionMs(
            request.AudioDurationMs,
            n,
            request.TransitionDurationMs,
            request.MinSegmentDurationMs);

        var slotTotalMs = request.AudioDurationMs + (transitionCount * effectiveTransitionMs);
        var slotDurations = DistributeSlotDurations(orderedAssets, slotTotalMs, request.MinSegmentDurationMs, effectiveTransitionMs);

        var transitionOut = ComputeTransitionOutDurations(slotDurations, effectiveTransitionMs);

        var segments = BuildSegments(orderedAssets, slotDurations, transitionOut);
        var availableVideoDurationMs = orderedAssets
            .Where(a => a.Type == BackgroundAssetType.Video)
            .Sum(a => a.SourceDurationMs ?? 0);

        var totalSlot = slotDurations.Sum();
        var totalOverlap = transitionOut.Sum();

        var timeline = new BackgroundTimeline
        {
            AudioDurationMs = request.AudioDurationMs,
            AudioDuration = FormatMs(request.AudioDurationMs),
            AvailableVideoDurationMs = availableVideoDurationMs,
            TransitionDurationMs = effectiveTransitionMs,
            OrderingMode = request.OrderingMode,
            OrderingSeed = request.OrderingSeed,
            OrderedAssetIds = orderedAssets.Select(a => a.Id).ToList(),
            Segments = segments,
            Summary = new BackgroundTimelineSummary
            {
                SegmentCount = segments.Count,
                ImageCount = orderedAssets.Count(a => a.Type == BackgroundAssetType.Image),
                VideoCount = orderedAssets.Count(a => a.Type == BackgroundAssetType.Video),
                TotalSlotDurationMs = totalSlot,
                TotalTransitionOverlapMs = totalOverlap,
                TimelineEndMs = segments.Count > 0 ? segments[^1].EndMs : 0
            }
        };

        var validationIssues = preflightIssues
            .Concat(_validationService.Validate(timeline, request).Issues)
            .ToList();

        return new BackgroundTimelineBuildResult
        {
            Timeline = timeline,
            Validation = new TimelineValidationResult { Issues = validationIssues }
        };
    }

    private static List<TimelineValidationIssue> PreflightValidate(BackgroundTimelineRequest request)
    {
        var issues = new List<TimelineValidationIssue>();

        if (request.AudioDurationMs <= 0)
        {
            issues.Add(Issue(
                TimelineValidationCode.InvalidAudioDuration,
                "Audio duration must be greater than zero.",
                TimelineValidationSeverity.Error));
        }

        if (request.Assets.Count == 0)
        {
            issues.Add(Issue(
                TimelineValidationCode.NoAssets,
                "At least one background asset is required.",
                TimelineValidationSeverity.Error));
            return issues;
        }

        if (request.TransitionDurationMs < 0)
        {
            issues.Add(Issue(
                TimelineValidationCode.InvalidTransitionDuration,
                "Transition duration cannot be negative.",
                TimelineValidationSeverity.Error));
        }

        var duplicateIds = request.Assets
            .GroupBy(a => a.Id)
            .Where(g => g.Count() > 1);

        foreach (var group in duplicateIds)
        {
            issues.Add(Issue(
                TimelineValidationCode.DuplicateAssetId,
                $"Duplicate asset id '{group.Key}'.",
                TimelineValidationSeverity.Error,
                assetId: group.Key));
        }

        foreach (var asset in request.Assets.Where(a => a.Type == BackgroundAssetType.Video))
        {
            if (asset.SourceDurationMs is null or <= 0)
            {
                issues.Add(Issue(
                    TimelineValidationCode.MissingVideoDuration,
                    $"Video asset '{asset.FileName}' ({asset.Id}) requires a positive source duration.",
                    TimelineValidationSeverity.Error,
                    assetId: asset.Id));
            }
        }

        return issues;
    }

    private static long ComputeEffectiveTransitionMs(
        long audioDurationMs,
        int segmentCount,
        long requestedTransitionMs,
        long minSegmentDurationMs)
    {
        if (segmentCount <= 1 || requestedTransitionMs == 0)
        {
            return 0;
        }

        // Each segment must accommodate the outgoing transition on both sides of a crossfade.
        var maxByAudio = audioDurationMs / segmentCount;
        var maxByMinSegment = minSegmentDurationMs;

        return Math.Min(requestedTransitionMs, Math.Max(0, Math.Min(maxByAudio, maxByMinSegment)));
    }

    /// <summary>
    /// Allocates slot durations: videos prefer their source duration; remaining time is split equally across images.
    /// Falls back to proportional equal split when video sources consume too much of the timeline.
    /// </summary>
    private static long[] DistributeSlotDurations(
        IReadOnlyList<BackgroundAsset> assets,
        long slotTotalMs,
        long minSegmentDurationMs,
        long transitionMs)
    {
        var n = assets.Count;
        var slots = new long[n];
        var minRequired = Math.Max(minSegmentDurationMs, transitionMs > 0 ? transitionMs : 1);

        if (n == 0)
        {
            return slots;
        }

        var imageIndices = Enumerable.Range(0, n).Where(i => assets[i].Type == BackgroundAssetType.Image).ToList();
        var videoIndices = Enumerable.Range(0, n).Where(i => assets[i].Type == BackgroundAssetType.Video).ToList();

        var videoSourceSum = videoIndices.Sum(i => assets[i].SourceDurationMs ?? 0);
        var imageMinTotal = imageIndices.Count * minRequired;

        if (imageIndices.Count == 0)
        {
            AllocateEqually(slots, n, slotTotalMs);
        }
        else if (videoIndices.Count == 0)
        {
            AllocateEqually(slots, imageIndices, slotTotalMs);
        }
        else if (videoSourceSum + imageMinTotal <= slotTotalMs)
        {
            foreach (var i in videoIndices)
            {
                slots[i] = assets[i].SourceDurationMs ?? minRequired;
            }

            var imageBudget = slotTotalMs - videoSourceSum;
            AllocateEqually(slots, imageIndices, imageBudget);
        }
        else
        {
            // Fallback: proportional split weighted by video source duration vs image baseline.
            var weights = assets.Select(a => a.Type switch
            {
                BackgroundAssetType.Video => (double)Math.Max(a.SourceDurationMs ?? minRequired, minRequired),
                _ => (double)minRequired * 2
            }).ToArray();

            var weightSum = weights.Sum();
            var allocated = 0L;

            for (var i = 0; i < n; i++)
            {
                slots[i] = (long)Math.Floor(slotTotalMs * weights[i] / weightSum);
                allocated += slots[i];
            }

            DistributeRemainder(slots, imageIndices.Count > 0 ? imageIndices : Enumerable.Range(0, n).ToList(), slotTotalMs - allocated);
        }

        EnforceMinimumDurations(assets, slots, minRequired, slotTotalMs);

        return slots;
    }

    private static void AllocateEqually(long[] slots, IReadOnlyList<int> indices, long totalMs)
    {
        if (indices.Count == 0)
        {
            return;
        }

        var baseAmount = totalMs / indices.Count;
        var allocated = 0L;

        foreach (var i in indices)
        {
            slots[i] = baseAmount;
            allocated += baseAmount;
        }

        DistributeRemainder(slots, indices, totalMs - allocated);
    }

    private static void AllocateEqually(long[] slots, int count, long totalMs)
    {
        AllocateEqually(slots, Enumerable.Range(0, count).ToList(), totalMs);
    }

    private static void DistributeRemainder(long[] slots, IReadOnlyList<int> indices, long remainder)
    {
        var i = 0;
        while (remainder > 0 && indices.Count > 0)
        {
            slots[indices[i % indices.Count]]++;
            remainder--;
            i++;
        }
    }

    private static void EnforceMinimumDurations(
        IReadOnlyList<BackgroundAsset> assets,
        long[] slots,
        long minRequired,
        long slotTotalMs)
    {
        var n = slots.Length;
        var deficit = 0L;

        for (var i = 0; i < n; i++)
        {
            if (slots[i] < minRequired)
            {
                deficit += minRequired - slots[i];
                slots[i] = minRequired;
            }
        }

        if (deficit <= 0)
        {
            return;
        }

        // Take surplus from largest image slots first (deterministic).
        var donors = Enumerable.Range(0, n)
            .Where(i => assets[i].Type == BackgroundAssetType.Image && slots[i] > minRequired)
            .OrderByDescending(i => slots[i])
            .ToList();

        foreach (var i in donors)
        {
            var available = slots[i] - minRequired;
            var take = Math.Min(available, deficit);
            slots[i] -= take;
            deficit -= take;
            if (deficit == 0)
            {
                break;
            }
        }

        if (deficit > 0)
        {
            // Last resort: reduce from largest slots regardless of type.
            var anyDonors = Enumerable.Range(0, n)
                .Where(i => slots[i] > minRequired)
                .OrderByDescending(i => slots[i]);

            foreach (var i in anyDonors)
            {
                var available = slots[i] - minRequired;
                var take = Math.Min(available, deficit);
                slots[i] -= take;
                deficit -= take;
                if (deficit == 0)
                {
                    break;
                }
            }
        }

        // Final normalization to exact slot total.
        var currentTotal = slots.Sum();
        var delta = slotTotalMs - currentTotal;
        if (delta != 0 && n > 0)
        {
            slots[^1] += delta;
        }
    }

    private static long[] ComputeTransitionOutDurations(long[] slotDurations, long effectiveTransitionMs)
    {
        var n = slotDurations.Length;
        var transitions = new long[n];

        if (effectiveTransitionMs == 0 || n <= 1)
        {
            return transitions;
        }

        for (var i = 0; i < n - 1; i++)
        {
            transitions[i] = Math.Min(effectiveTransitionMs, Math.Min(slotDurations[i], slotDurations[i + 1]));
        }

        return transitions;
    }

    private static List<TimelineSegment> BuildSegments(
        IReadOnlyList<BackgroundAsset> assets,
        long[] slotDurations,
        long[] transitionOut)
    {
        var segments = new List<TimelineSegment>(assets.Count);
        long cursor = 0;

        for (var i = 0; i < assets.Count; i++)
        {
            var asset = assets[i];
            var slot = slotDurations[i];
            var startMs = cursor;
            var endMs = startMs + slot;

            var playback = ResolveVideoPlayback(asset, slot, out var loopCount, out var trimStart, out var sourcePlayDuration);

            segments.Add(new TimelineSegment
            {
                Sequence = i + 1,
                AssetId = asset.Id,
                FileName = asset.FileName,
                AssetType = asset.Type,
                StartMs = startMs,
                EndMs = endMs,
                Start = FormatMs(startMs),
                End = FormatMs(endMs),
                SlotDurationMs = slot,
                TransitionOutDurationMs = transitionOut[i],
                PlaybackMode = playback,
                LoopCount = loopCount,
                TrimStartMs = trimStart,
                SourcePlayDurationMs = sourcePlayDuration,
                SourceDurationMs = asset.SourceDurationMs
            });

            cursor = endMs - transitionOut[i];
        }

        return segments;
    }

    private static VideoPlaybackMode ResolveVideoPlayback(
        BackgroundAsset asset,
        long slotMs,
        out int loopCount,
        out long trimStartMs,
        out long? sourcePlayDurationMs)
    {
        loopCount = 1;
        trimStartMs = 0;
        sourcePlayDurationMs = null;

        if (asset.Type != BackgroundAssetType.Video || asset.SourceDurationMs is null or <= 0)
        {
            return VideoPlaybackMode.Normal;
        }

        var source = asset.SourceDurationMs.Value;

        if (slotMs > source)
        {
            loopCount = (int)Math.Ceiling(slotMs / (double)source);
            sourcePlayDurationMs = source;
            return VideoPlaybackMode.Looped;
        }

        if (slotMs < source)
        {
            sourcePlayDurationMs = slotMs;
            return VideoPlaybackMode.Trimmed;
        }

        sourcePlayDurationMs = source;
        return VideoPlaybackMode.Normal;
    }

    internal static List<BackgroundAsset> OrderAssets(
        IReadOnlyList<BackgroundAsset> assets,
        AssetOrderingMode mode,
        int seed)
    {
        List<BackgroundAsset> ordered = mode switch
        {
            AssetOrderingMode.Manual => assets
                .OrderBy(a => a.SortOrder)
                .ThenBy(a => a.FileName, StringComparer.Ordinal)
                .ThenBy(a => a.Id, StringComparer.Ordinal)
                .ToList(),
            AssetOrderingMode.Automatic => DeterministicShuffle(assets.ToList(), seed),
            _ => assets.ToList()
        };

        return RemoveAdjacentDuplicateImages(ordered);
    }

    internal static List<BackgroundAsset> DeterministicShuffle(List<BackgroundAsset> assets, int seed)
    {
        var rng = new Random(seed);
        for (var i = assets.Count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (assets[i], assets[j]) = (assets[j], assets[i]);
        }

        return assets;
    }

    internal static List<BackgroundAsset> RemoveAdjacentDuplicateImages(List<BackgroundAsset> assets)
    {
        if (assets.Count <= 1)
        {
            return assets;
        }

        for (var i = 1; i < assets.Count; i++)
        {
            if (!IsSameImageAsset(assets[i - 1], assets[i]))
            {
                continue;
            }

            var swapIndex = -1;
            for (var j = i + 1; j < assets.Count; j++)
            {
                if (!IsSameImageAsset(assets[i - 1], assets[j]))
                {
                    swapIndex = j;
                    break;
                }
            }

            if (swapIndex >= 0)
            {
                (assets[i], assets[swapIndex]) = (assets[swapIndex], assets[i]);
            }
        }

        return assets;
    }

    private static bool IsSameImageAsset(BackgroundAsset a, BackgroundAsset b) =>
        a.Type == BackgroundAssetType.Image &&
        b.Type == BackgroundAssetType.Image &&
        (string.Equals(a.Id, b.Id, StringComparison.Ordinal) ||
         string.Equals(a.FileName, b.FileName, StringComparison.OrdinalIgnoreCase));

    private static string FormatMs(long ms) => new Timecode(ms).ToDisplayString();

    private static BackgroundTimeline CreateEmptyTimeline(BackgroundTimelineRequest request) => new()
    {
        AudioDurationMs = request.AudioDurationMs,
        AudioDuration = FormatMs(Math.Max(request.AudioDurationMs, 0)),
        AvailableVideoDurationMs = 0,
        TransitionDurationMs = request.TransitionDurationMs,
        OrderingMode = request.OrderingMode,
        OrderingSeed = request.OrderingSeed,
        OrderedAssetIds = [],
        Segments = [],
        Summary = new BackgroundTimelineSummary
        {
            SegmentCount = 0,
            ImageCount = 0,
            VideoCount = 0,
            TotalSlotDurationMs = 0,
            TotalTransitionOverlapMs = 0,
            TimelineEndMs = 0
        }
    };

    private static TimelineValidationIssue Issue(
        string code,
        string message,
        TimelineValidationSeverity severity,
        int? segmentSequence = null,
        string? assetId = null) => new()
    {
        Code = code,
        Message = message,
        Severity = severity,
        SegmentSequence = segmentSequence,
        AssetId = assetId
    };
}
