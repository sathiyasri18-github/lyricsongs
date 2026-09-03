namespace Lyricsongs.Domain.Rendering;

public sealed class FfmpegExecutionResult
{
    public required int ExitCode { get; init; }
    public required IReadOnlyList<string> Arguments { get; init; }
    public required string CommandLine { get; init; }
    public required string StandardError { get; init; }
    public required string StandardOutput { get; init; }
    public required TimeSpan Elapsed { get; init; }

    public bool IsSuccess => ExitCode == 0;
}
