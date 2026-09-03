namespace Lyricsongs.Domain.Rendering;

public sealed class RenderProgressReport
{
    public required string JobId { get; init; }
    public required RenderStage Stage { get; init; }
    public required double OverallPercent { get; init; }
    public required double StagePercent { get; init; }
    public long? ProcessedDurationMs { get; init; }
    public long? TotalDurationMs { get; init; }
    public string? Message { get; init; }
}
