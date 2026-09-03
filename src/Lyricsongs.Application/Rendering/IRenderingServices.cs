using Lyricsongs.Domain.Rendering;

namespace Lyricsongs.Application.Rendering;

public sealed class FfmpegOptions
{
    public string ExecutablePath { get; set; } = "ffmpeg";
    public string? FFprobeExecutablePath { get; set; }

    /// <summary>
    /// Encoding preference: Auto (prefer GPU), Gpu (require hardware), or Cpu (libx264).
    /// </summary>
    public string HardwareEncoding { get; set; } = "Auto";

    /// <summary>
    /// Optional forced encoder (e.g. h264_nvenc, h264_amf, h264_qsv, h264_mf, libx264).
    /// When empty, the encoder is chosen from <see cref="HardwareEncoding"/>.
    /// </summary>
    public string? VideoEncoder { get; set; }

    /// <summary>Resolved encoder after startup detection.</summary>
    public string ResolvedVideoEncoder { get; set; } = "libx264";

    public bool IsHardwareEncoder { get; set; }
}

public interface IVideoRenderEngine
{
    Task<RenderResult> RenderAsync(
        VideoRenderRequest request,
        IProgress<RenderProgressReport>? progress = null,
        CancellationToken cancellationToken = default);
}

public interface IRenderPipelinePlanner
{
    RenderPipelinePlan Plan(VideoRenderRequest request, string workingDirectory);
}

public interface IFfmpegCommandBuilder
{
    IReadOnlyList<string> BuildSegmentImageCommand(
        SegmentRenderPlan segment,
        RenderOutputConfiguration output);

    IReadOnlyList<string> BuildSegmentVideoCommand(
        SegmentRenderPlan segment,
        RenderOutputConfiguration output);

    IReadOnlyList<string> BuildXfadeConcatCommand(
        IReadOnlyList<string> segmentPaths,
        IReadOnlyList<double> segmentDurationsSec,
        IReadOnlyList<long> transitionDurationsMs,
        double totalDurationSeconds,
        string outputPath,
        RenderOutputConfiguration output);

    IReadOnlyList<string> BuildConcatDemuxerCommand(
        string concatListPath,
        string outputPath);

    IReadOnlyList<string> BuildFinalMuxCommand(
        string backgroundVideoPath,
        string assFilePath,
        string audioFilePath,
        string outputPath,
        RenderOutputConfiguration output);
}

public interface IFfmpegFilterGraphBuilder
{
    string BuildKenBurnsFilter(KenBurnsMotion motion, int width, int height, int frameRate, int frameCount);
    string BuildScalePadFilter(int width, int height, int frameRate);
    string BuildAssOverlayFilter(string assFilePath);
    string BuildXfadeFilterComplex(IReadOnlyList<double> segmentDurationsSec, IReadOnlyList<double> transitionDurationsSec, string transitionName);
}

public interface IFfmpegProcessRunner
{
    Task<FfmpegExecutionResult> RunAsync(
        IReadOnlyList<string> arguments,
        long? totalDurationMs = null,
        IProgress<FfmpegProgressUpdate>? progress = null,
        CancellationToken cancellationToken = default);
}

public interface IFfmpegProgressParser
{
    FfmpegProgressUpdate? ParseProgressLine(string line);
    FfmpegProgressUpdate? ParseStatsLine(string line);
}

public sealed class FfmpegProgressUpdate
{
    public long? ProcessedDurationMs { get; init; }
    public long? Frame { get; init; }
    public double? Speed { get; init; }
    public string? RawLine { get; init; }
}

public interface ITemporaryRenderDirectory : IAsyncDisposable
{
    string Path { get; }
    string CreateFile(string fileName);
    Task CleanupAsync(CancellationToken cancellationToken = default);
}

public interface ITemporaryRenderDirectoryFactory
{
    Task<ITemporaryRenderDirectory> CreateAsync(string jobId, string? basePath = null, CancellationToken cancellationToken = default);
}

public interface IFfmpegHealthCheck
{
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
}
