namespace Lyricsongs.Domain.Rendering;

public sealed class RenderResult
{
    public required string JobId { get; init; }
    public required bool Success { get; init; }
    public string? OutputFilePath { get; init; }
    public RenderStage FinalStage { get; init; }
    public string? ErrorMessage { get; init; }
    public IReadOnlyList<string> ExecutedCommands { get; init; } = [];
    public IReadOnlyList<string> IntermediateFiles { get; init; } = [];
    public TimeSpan Elapsed { get; init; }
}
