namespace Lyricsongs.Domain.Rendering;

public sealed class FfmpegRenderException : Exception
{
    public string? CommandLine { get; }
    public int ExitCode { get; }
    public string? StandardError { get; }

    public FfmpegRenderException(string message, string? commandLine, int exitCode, string? standardError)
        : base(message)
    {
        CommandLine = commandLine;
        ExitCode = exitCode;
        StandardError = standardError;
    }
}
