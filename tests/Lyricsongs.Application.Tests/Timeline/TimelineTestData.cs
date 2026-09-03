using Lyricsongs.Application.Timeline;
using Lyricsongs.Domain.Timeline;

namespace Lyricsongs.Application.Tests.Timeline;

internal static class TimelineTestData
{
    public static BackgroundTimelineRequest CreateExampleRequest(
        long transitionMs = 0,
        AssetOrderingMode ordering = AssetOrderingMode.Manual) => new()
    {
        AudioDurationMs = 245_000,
        TransitionDurationMs = transitionMs,
        MinSegmentDurationMs = 3_000,
        OrderingMode = ordering,
        OrderingSeed = 42,
        Assets =
        [
            Asset("bg1", "bg1.jpg", BackgroundAssetType.Image, sortOrder: 0),
            Asset("clip1", "clip1.mp4", BackgroundAssetType.Video, 35_000, sortOrder: 1),
            Asset("bg2", "bg2.jpg", BackgroundAssetType.Image, sortOrder: 2),
            Asset("bg3", "bg3.jpg", BackgroundAssetType.Image, sortOrder: 3),
            Asset("clip2", "clip2.mp4", BackgroundAssetType.Video, 40_000, sortOrder: 4),
            Asset("bg4", "bg4.jpg", BackgroundAssetType.Image, sortOrder: 5)
        ]
    };

    public static BackgroundAsset Asset(
        string id,
        string fileName,
        BackgroundAssetType type,
        long? sourceDurationMs = null,
        int sortOrder = 0) => new()
    {
        Id = id,
        FileName = fileName,
        Type = type,
        SourceDurationMs = sourceDurationMs,
        SortOrder = sortOrder
    };
}
