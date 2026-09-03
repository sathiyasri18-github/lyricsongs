namespace Lyricsongs.Domain.Timeline;

/// <summary>
/// A background image or video clip available for timeline allocation.
/// </summary>
public sealed class BackgroundAsset
{
    public required string Id { get; init; }
    public required string FileName { get; init; }
    public required BackgroundAssetType Type { get; init; }

    /// <summary>
    /// Source duration in milliseconds. Required for videos; null for images.
    /// </summary>
    public long? SourceDurationMs { get; init; }

    /// <summary>Manual ordering position (lower first). Used when ordering mode is Manual.</summary>
    public int SortOrder { get; init; }
}
