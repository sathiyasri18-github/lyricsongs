namespace Lyricsongs.Domain.Subtitles;

public sealed class SrtValidationIssue
{
    public required string Code { get; init; }
    public required string Message { get; init; }
    public required SrtValidationSeverity Severity { get; init; }
    public int? CueIndex { get; init; }
    public int? RelatedCueIndex { get; init; }
    public int? LineNumber { get; init; }
}
