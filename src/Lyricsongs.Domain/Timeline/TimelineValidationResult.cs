namespace Lyricsongs.Domain.Timeline;

public sealed class TimelineValidationResult
{
    public required IReadOnlyList<TimelineValidationIssue> Issues { get; init; }

    public bool IsValid => Issues.All(i => i.Severity != TimelineValidationSeverity.Error);
    public bool HasWarnings => Issues.Any(i => i.Severity == TimelineValidationSeverity.Warning);
}
