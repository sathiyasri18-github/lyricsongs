namespace Lyricsongs.Domain.Timeline;

public enum AssetOrderingMode
{
    /// <summary>Use explicit <see cref="BackgroundAsset.SortOrder"/> values.</summary>
    Manual = 0,

    /// <summary>Deterministic shuffle using <see cref="BackgroundTimelineRequest.OrderingSeed"/>.</summary>
    Automatic = 1
}
