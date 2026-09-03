namespace Lyricsongs.Domain.Rendering;

public sealed class RenderPipelinePlan
{
    public required string WorkingDirectory { get; init; }
    public required string AssFilePath { get; init; }
    public required string BackgroundVideoPath { get; init; }
    public required string OutputFilePath { get; init; }
    public required IReadOnlyList<SegmentRenderPlan> Segments { get; init; }
    public required long TotalDurationMs { get; init; }
}

public sealed class SegmentRenderPlan
{
    public required int Sequence { get; init; }
    public required string SourceAssetPath { get; init; }
    public required string OutputSegmentPath { get; init; }
    public required double DurationSeconds { get; init; }
    public required bool IsImage { get; init; }
    public KenBurnsMotion KenBurnsMotion { get; init; }
    public int LoopCount { get; init; } = 1;
    public double TrimStartSeconds { get; init; }
    public long TransitionOutDurationMs { get; init; }
}
