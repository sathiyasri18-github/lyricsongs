using Lyricsongs.Domain.Timeline;

namespace Lyricsongs.Domain.VideoPlan;

/// <summary>
/// Uploaded background asset available for planning.
/// </summary>
public sealed class VideoPlanAsset
{
    public required string AssetId { get; init; }
    public required string FileName { get; init; }
    public required BackgroundAssetType Type { get; init; }

    /// <summary>Source duration in ms. Required for videos; null for images.</summary>
    public long? SourceDurationMs { get; init; }

    public int SortOrder { get; init; }
}
