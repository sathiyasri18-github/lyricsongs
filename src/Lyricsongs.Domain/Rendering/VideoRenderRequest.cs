using Lyricsongs.Domain.Lyrics;
using Lyricsongs.Domain.Timeline;

namespace Lyricsongs.Domain.Rendering;

public sealed class VideoRenderRequest
{
    public required string JobId { get; init; }
    public required string AudioFilePath { get; init; }
    public required BackgroundTimeline Timeline { get; init; }
    public required IReadOnlyDictionary<string, string> AssetPathMap { get; init; }
    public required LyricRenderPlan LyricPlan { get; init; }
    public required RenderOutputConfiguration Output { get; init; }
    public string? OutputFilePath { get; init; }
    public string? WorkingDirectory { get; init; }
}
