namespace Lyricsongs.Domain.Timeline;

public sealed class TimelineValidationIssue
{
    public required string Code { get; init; }
    public required string Message { get; init; }
    public required TimelineValidationSeverity Severity { get; init; }
    public int? SegmentSequence { get; init; }
    public string? AssetId { get; init; }
}
