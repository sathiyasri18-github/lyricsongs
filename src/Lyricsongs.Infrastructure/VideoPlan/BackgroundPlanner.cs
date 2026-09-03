using Lyricsongs.Application.VideoPlan;
using Lyricsongs.Domain.Timeline;
using Lyricsongs.Domain.VideoPlan;

namespace Lyricsongs.Infrastructure.VideoPlan;

/// <summary>
/// Deterministic background distribution across the audio duration.
/// Modes: Sequential, Random (seeded), Balanced.
/// </summary>
public sealed class BackgroundPlanner : IBackgroundPlanner
{
    private static readonly BackgroundAnimation[] CycleAnimations =
    [
        BackgroundAnimation.ZoomIn,
        BackgroundAnimation.ZoomOut,
        BackgroundAnimation.PanLeft,
        BackgroundAnimation.PanRight,
        BackgroundAnimation.PanUp,
        BackgroundAnimation.PanDown,
        BackgroundAnimation.SlowZoomIn,
        BackgroundAnimation.SlowZoomOut
    ];

    public IReadOnlyList<BackgroundSegment> Plan(
        long audioDurationMs,
        IReadOnlyList<VideoPlanAsset> assets,
        VideoPlanSettings settings)
    {
        ArgumentNullException.ThrowIfNull(assets);
        ArgumentNullException.ThrowIfNull(settings);

        if (audioDurationMs <= 0)
        {
            throw new VideoPlanException("Audio duration must be greater than zero.");
        }

        if (assets.Count == 0)
        {
            throw new VideoPlanException("At least one background asset is required.");
        }

        if (settings.ManualBackgrounds is { Count: > 0 })
        {
            return BuildFromManualOverrides(assets, settings);
        }

        var ordered = OrderAssets(assets, settings);
        ordered = AvoidAdjacentDuplicates(ordered);

        var transitionMs = ResolveTransitionDuration(settings);
        var n = ordered.Count;
        var transitionCount = Math.Max(0, n - 1);
        var slotTotalMs = audioDurationMs + (transitionCount * transitionMs);

        var slotDurations = DistributeSlots(ordered, slotTotalMs, settings.MinSegmentDurationMs, transitionMs);
        var transitionsOut = ComputeTransitions(slotDurations, settings, transitionMs);

        return BuildSegments(ordered, slotDurations, transitionsOut, settings);
    }

    private static List<BackgroundSegment> BuildFromManualOverrides(
        IReadOnlyList<VideoPlanAsset> assets,
        VideoPlanSettings settings)
    {
        var byId = assets.ToDictionary(a => a.AssetId, StringComparer.Ordinal);
        var segments = new List<BackgroundSegment>();
        var imageAnimIndex = 0;

        foreach (var manual in settings.ManualBackgrounds!)
        {
            if (!byId.TryGetValue(manual.AssetId, out var asset))
            {
                throw new VideoPlanException($"Unknown asset id '{manual.AssetId}' in manual override.");
            }

            var duration = manual.EndMs - manual.StartMs;
            var animation = manual.Animation
                ?? ResolveAnimation(asset, imageAnimIndex, settings);

            if (asset.Type == BackgroundAssetType.Image)
            {
                imageAnimIndex++;
            }

            var (playback, loop, trim, playDur) = ResolveVideoPlayback(asset, duration);
            var transitionOut = manual.TransitionOut ?? settings.Transition.Type;
            var transitionOutMs = manual.TransitionOutDurationMs
                ?? (transitionOut == TransitionType.None ? 0 : settings.Transition.DurationMs);

            segments.Add(new BackgroundSegment
            {
                AssetId = asset.AssetId,
                Type = asset.Type,
                StartMs = manual.StartMs,
                EndMs = manual.EndMs,
                Animation = animation,
                TransitionOut = transitionOut,
                TransitionOutDurationMs = transitionOutMs,
                PlaybackMode = playback,
                LoopCount = loop,
                TrimStartMs = trim,
                SourcePlayDurationMs = playDur,
                SourceDurationMs = asset.SourceDurationMs
            });
        }

        // Last segment: no outgoing transition
        if (segments.Count > 0)
        {
            var last = segments[^1];
            segments[^1] = CloneSegment(last, TransitionType.None, 0);
        }

        return segments;
    }

    private static List<VideoPlanAsset> OrderAssets(
        IReadOnlyList<VideoPlanAsset> assets,
        VideoPlanSettings settings)
    {
        if (settings.AssetOrderOverride is { Count: > 0 })
        {
            var byId = assets.ToDictionary(a => a.AssetId, StringComparer.Ordinal);
            var ordered = new List<VideoPlanAsset>();
            foreach (var id in settings.AssetOrderOverride)
            {
                if (!byId.TryGetValue(id, out var asset))
                {
                    throw new VideoPlanException($"Asset order override references unknown id '{id}'.");
                }

                ordered.Add(asset);
            }

            return ordered;
        }

        return settings.PlanningMode switch
        {
            BackgroundPlanningMode.Sequential => assets
                .OrderBy(a => a.SortOrder)
                .ThenBy(a => a.FileName, StringComparer.Ordinal)
                .ThenBy(a => a.AssetId, StringComparer.Ordinal)
                .ToList(),
            BackgroundPlanningMode.Random => DeterministicShuffle(assets.ToList(), settings.RandomSeed),
            BackgroundPlanningMode.Balanced => BalanceAssets(assets),
            _ => assets.ToList()
        };
    }

    /// <summary>
    /// Interleave images and videos for visual variety (deterministic).
    /// </summary>
    private static List<VideoPlanAsset> BalanceAssets(IReadOnlyList<VideoPlanAsset> assets)
    {
        var images = assets
            .Where(a => a.Type == BackgroundAssetType.Image)
            .OrderBy(a => a.SortOrder)
            .ThenBy(a => a.FileName, StringComparer.Ordinal)
            .ToList();

        var videos = assets
            .Where(a => a.Type == BackgroundAssetType.Video)
            .OrderBy(a => a.SortOrder)
            .ThenBy(a => a.FileName, StringComparer.Ordinal)
            .ToList();

        var result = new List<VideoPlanAsset>(assets.Count);
        var i = 0;
        var v = 0;

        while (i < images.Count || v < videos.Count)
        {
            if (i < images.Count)
            {
                result.Add(images[i++]);
            }

            if (v < videos.Count)
            {
                result.Add(videos[v++]);
            }
        }

        return result;
    }

    private static List<VideoPlanAsset> DeterministicShuffle(List<VideoPlanAsset> assets, int seed)
    {
        var rng = new Random(seed);
        for (var i = assets.Count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (assets[i], assets[j]) = (assets[j], assets[i]);
        }

        return assets;
    }

    private static List<VideoPlanAsset> AvoidAdjacentDuplicates(List<VideoPlanAsset> assets)
    {
        for (var i = 1; i < assets.Count; i++)
        {
            if (!IsSameAsset(assets[i - 1], assets[i]))
            {
                continue;
            }

            for (var j = i + 1; j < assets.Count; j++)
            {
                if (!IsSameAsset(assets[i - 1], assets[j]))
                {
                    (assets[i], assets[j]) = (assets[j], assets[i]);
                    break;
                }
            }
        }

        return assets;
    }

    private static bool IsSameAsset(VideoPlanAsset a, VideoPlanAsset b) =>
        string.Equals(a.AssetId, b.AssetId, StringComparison.Ordinal) ||
        (a.Type == BackgroundAssetType.Image &&
         b.Type == BackgroundAssetType.Image &&
         string.Equals(a.FileName, b.FileName, StringComparison.OrdinalIgnoreCase));

    private static long ResolveTransitionDuration(VideoPlanSettings settings)
    {
        if (settings.Transition.Type == TransitionType.None || settings.Transition.DurationMs <= 0)
        {
            return 0;
        }

        return settings.Transition.DurationMs;
    }

    private static long[] DistributeSlots(
        IReadOnlyList<VideoPlanAsset> assets,
        long slotTotalMs,
        long minSegmentDurationMs,
        long transitionMs)
    {
        var n = assets.Count;
        var slots = new long[n];
        var minRequired = Math.Max(minSegmentDurationMs, transitionMs > 0 ? transitionMs : 1);

        var imageIndices = Enumerable.Range(0, n).Where(i => assets[i].Type == BackgroundAssetType.Image).ToList();
        var videoIndices = Enumerable.Range(0, n).Where(i => assets[i].Type == BackgroundAssetType.Video).ToList();

        var videoSourceSum = videoIndices.Sum(i => assets[i].SourceDurationMs ?? 0);
        var imageMinTotal = imageIndices.Count * minRequired;

        if (imageIndices.Count == 0)
        {
            AllocateEqually(slots, Enumerable.Range(0, n).ToList(), slotTotalMs);
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

            AllocateEqually(slots, imageIndices, slotTotalMs - videoSourceSum);
        }
        else
        {
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

        EnforceMinimums(assets, slots, minRequired, slotTotalMs);
        return slots;
    }

    private static void AllocateEqually(long[] slots, IReadOnlyList<int> indices, long totalMs)
    {
        if (indices.Count == 0)
        {
            return;
        }

        var baseAmount = totalMs / indices.Count;
        foreach (var i in indices)
        {
            slots[i] = baseAmount;
        }

        DistributeRemainder(slots, indices, totalMs - (baseAmount * indices.Count));
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

    private static void EnforceMinimums(
        IReadOnlyList<VideoPlanAsset> assets,
        long[] slots,
        long minRequired,
        long slotTotalMs)
    {
        var deficit = 0L;
        for (var i = 0; i < slots.Length; i++)
        {
            if (slots[i] < minRequired)
            {
                deficit += minRequired - slots[i];
                slots[i] = minRequired;
            }
        }

        if (deficit > 0)
        {
            var donors = Enumerable.Range(0, slots.Length)
                .Where(i => slots[i] > minRequired)
                .OrderByDescending(i => assets[i].Type == BackgroundAssetType.Image ? 1 : 0)
                .ThenByDescending(i => slots[i]);

            foreach (var i in donors)
            {
                var take = Math.Min(slots[i] - minRequired, deficit);
                slots[i] -= take;
                deficit -= take;
                if (deficit == 0)
                {
                    break;
                }
            }
        }

        var delta = slotTotalMs - slots.Sum();
        if (delta != 0 && slots.Length > 0)
        {
            slots[^1] += delta;
        }
    }

    private static long[] ComputeTransitions(long[] slotDurations, VideoPlanSettings settings, long transitionMs)
    {
        var transitions = new long[slotDurations.Length];
        if (settings.Transition.Type == TransitionType.None || transitionMs == 0 || slotDurations.Length <= 1)
        {
            return transitions;
        }

        for (var i = 0; i < slotDurations.Length - 1; i++)
        {
            transitions[i] = Math.Min(transitionMs, Math.Min(slotDurations[i], slotDurations[i + 1]));
        }

        return transitions;
    }

    private static List<BackgroundSegment> BuildSegments(
        IReadOnlyList<VideoPlanAsset> assets,
        long[] slotDurations,
        long[] transitionsOut,
        VideoPlanSettings settings)
    {
        var segments = new List<BackgroundSegment>(assets.Count);
        long cursor = 0;
        var imageAnimIndex = 0;

        for (var i = 0; i < assets.Count; i++)
        {
            var asset = assets[i];
            var slot = slotDurations[i];
            var startMs = cursor;
            var endMs = startMs + slot;
            var animation = ResolveAnimation(asset, imageAnimIndex, settings);

            if (asset.Type == BackgroundAssetType.Image)
            {
                imageAnimIndex++;
            }

            var (playback, loop, trim, playDur) = ResolveVideoPlayback(asset, slot);

            segments.Add(new BackgroundSegment
            {
                AssetId = asset.AssetId,
                Type = asset.Type,
                StartMs = startMs,
                EndMs = endMs,
                Animation = animation,
                TransitionOut = i < assets.Count - 1 && transitionsOut[i] > 0
                    ? settings.Transition.Type
                    : TransitionType.None,
                TransitionOutDurationMs = i < assets.Count - 1 ? transitionsOut[i] : 0,
                PlaybackMode = playback,
                LoopCount = loop,
                TrimStartMs = trim,
                SourcePlayDurationMs = playDur,
                SourceDurationMs = asset.SourceDurationMs
            });

            cursor = endMs - transitionsOut[i];
        }

        return segments;
    }

    private static BackgroundAnimation ResolveAnimation(
        VideoPlanAsset asset,
        int imageAnimIndex,
        VideoPlanSettings settings)
    {
        if (asset.Type == BackgroundAssetType.Video)
        {
            return BackgroundAnimation.None;
        }

        if (!settings.CycleImageAnimations)
        {
            return settings.DefaultImageAnimation;
        }

        return CycleAnimations[imageAnimIndex % CycleAnimations.Length];
    }

    private static (VideoPlaybackMode Mode, int LoopCount, long TrimStartMs, long? SourcePlayDurationMs)
        ResolveVideoPlayback(VideoPlanAsset asset, long slotMs)
    {
        if (asset.Type != BackgroundAssetType.Video || asset.SourceDurationMs is null or <= 0)
        {
            return (VideoPlaybackMode.Normal, 1, 0, null);
        }

        var source = asset.SourceDurationMs.Value;
        if (slotMs > source)
        {
            return (VideoPlaybackMode.Looped, (int)Math.Ceiling(slotMs / (double)source), 0, source);
        }

        if (slotMs < source)
        {
            return (VideoPlaybackMode.Trimmed, 1, 0, slotMs);
        }

        return (VideoPlaybackMode.Normal, 1, 0, source);
    }

    private static BackgroundSegment CloneSegment(
        BackgroundSegment source,
        TransitionType transitionOut,
        long transitionOutMs) => new()
    {
        AssetId = source.AssetId,
        Type = source.Type,
        StartMs = source.StartMs,
        EndMs = source.EndMs,
        Animation = source.Animation,
        TransitionOut = transitionOut,
        TransitionOutDurationMs = transitionOutMs,
        PlaybackMode = source.PlaybackMode,
        LoopCount = source.LoopCount,
        TrimStartMs = source.TrimStartMs,
        SourcePlayDurationMs = source.SourcePlayDurationMs,
        SourceDurationMs = source.SourceDurationMs
    };
}
