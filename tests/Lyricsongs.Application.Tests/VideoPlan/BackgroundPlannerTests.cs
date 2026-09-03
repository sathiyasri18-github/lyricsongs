using Lyricsongs.Application.VideoPlan;
using Lyricsongs.Domain.Lyrics;
using Lyricsongs.Domain.Timeline;
using Lyricsongs.Domain.VideoPlan;
using Lyricsongs.Infrastructure.VideoPlan;

namespace Lyricsongs.Application.Tests.VideoPlan;

public class BackgroundPlannerTests
{
    private readonly BackgroundPlanner _planner = new();

    [Fact]
    public void Plan_Balanced_InterleavesImagesAndVideos()
    {
        var assets = new List<VideoPlanAsset>
        {
            Asset("bg1", BackgroundAssetType.Image, null, 0),
            Asset("bg2", BackgroundAssetType.Image, null, 1),
            Asset("clip1", BackgroundAssetType.Video, 10_000, 2),
            Asset("clip2", BackgroundAssetType.Video, 10_000, 3)
        };

        var segments = _planner.Plan(60_000, assets, new VideoPlanSettings
        {
            PlanningMode = BackgroundPlanningMode.Balanced,
            Transition = TransitionSettings.None
        });

        Assert.Equal(["bg1", "clip1", "bg2", "clip2"], segments.Select(s => s.AssetId).ToList());
    }

    [Fact]
    public void Plan_AvoidsImmediateRepeatWhenPossible()
    {
        var assets = new List<VideoPlanAsset>
        {
            Asset("bg1a", BackgroundAssetType.Image, null, 0, "same.jpg"),
            Asset("bg1b", BackgroundAssetType.Image, null, 1, "same.jpg"),
            Asset("bg2", BackgroundAssetType.Image, null, 2, "other.jpg")
        };

        var segments = _planner.Plan(30_000, assets, new VideoPlanSettings
        {
            PlanningMode = BackgroundPlanningMode.Sequential,
            Transition = TransitionSettings.None
        });

        Assert.NotEqual(segments[0].AssetId, segments[1].AssetId);
    }

    private static VideoPlanAsset Asset(
        string id,
        BackgroundAssetType type,
        long? duration,
        int order,
        string? fileName = null) => new()
    {
        AssetId = id,
        FileName = fileName ?? $"{id}.{(type == BackgroundAssetType.Image ? "jpg" : "mp4")}",
        Type = type,
        SourceDurationMs = duration,
        SortOrder = order
    };
}
