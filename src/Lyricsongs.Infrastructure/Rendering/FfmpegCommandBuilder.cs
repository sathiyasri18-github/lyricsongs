using System.Globalization;
using Lyricsongs.Application.Rendering;
using Lyricsongs.Domain.Rendering;

namespace Lyricsongs.Infrastructure.Rendering;

public sealed class FfmpegCommandBuilder : IFfmpegCommandBuilder
{
    private readonly IFfmpegFilterGraphBuilder _filterGraphBuilder;

    public FfmpegCommandBuilder(IFfmpegFilterGraphBuilder filterGraphBuilder)
    {
        _filterGraphBuilder = filterGraphBuilder;
    }

    public IReadOnlyList<string> BuildSegmentImageCommand(
        SegmentRenderPlan segment,
        RenderOutputConfiguration output)
    {
        var frameCount = (int)Math.Ceiling(segment.DurationSeconds * output.FrameRate);
        var vf = _filterGraphBuilder.BuildKenBurnsFilter(
            segment.KenBurnsMotion,
            output.Width,
            output.Height,
            output.FrameRate,
            Math.Max(1, frameCount));

        var args = new List<string>
        {
            "-hide_banner", "-y",
            "-loop", "1",
            "-i", segment.SourceAssetPath,
            "-t", segment.DurationSeconds.ToString("F3", CultureInfo.InvariantCulture),
            "-vf", vf
        };
        args.AddRange(FfmpegVideoEncodeArgs.Build(output.VideoCodec, output.VideoPreset, output.VideoCrf));
        args.AddRange([
            "-r", output.FrameRate.ToString(CultureInfo.InvariantCulture),
            "-an",
            segment.OutputSegmentPath
        ]);
        return args;
    }

    public IReadOnlyList<string> BuildSegmentVideoCommand(
        SegmentRenderPlan segment,
        RenderOutputConfiguration output)
    {
        var args = new List<string>
        {
            "-hide_banner", "-y",
            "-hwaccel", "auto"
        };

        if (segment.LoopCount > 1)
        {
            args.Add("-stream_loop");
            args.Add((segment.LoopCount - 1).ToString(CultureInfo.InvariantCulture));
        }

        args.Add("-i");
        args.Add(segment.SourceAssetPath);

        if (segment.TrimStartSeconds > 0)
        {
            args.Add("-ss");
            args.Add(segment.TrimStartSeconds.ToString("F3", CultureInfo.InvariantCulture));
        }

        args.Add("-t");
        args.Add(segment.DurationSeconds.ToString("F3", CultureInfo.InvariantCulture));

        var vf = _filterGraphBuilder.BuildScalePadFilter(output.Width, output.Height, output.FrameRate);
        args.Add("-vf");
        args.Add(vf);
        args.AddRange(FfmpegVideoEncodeArgs.Build(output.VideoCodec, output.VideoPreset, output.VideoCrf));
        args.Add("-an");
        args.Add(segment.OutputSegmentPath);

        return args;
    }

    public IReadOnlyList<string> BuildXfadeConcatCommand(
        IReadOnlyList<string> segmentPaths,
        IReadOnlyList<double> segmentDurationsSec,
        IReadOnlyList<long> transitionDurationsMs,
        double totalDurationSeconds,
        string outputPath,
        RenderOutputConfiguration output)
    {
        if (segmentPaths.Count == 1)
        {
            return
            [
                "-hide_banner", "-y",
                "-i", segmentPaths[0],
                "-c", "copy",
                outputPath
            ];
        }

        var args = new List<string> { "-hide_banner", "-y" };
        foreach (var path in segmentPaths)
        {
            args.Add("-i");
            args.Add(path);
        }

        var transitionSec = transitionDurationsMs.Select(ms => ms / 1000.0).ToList();
        var filter = _filterGraphBuilder.BuildXfadeFilterComplex(segmentDurationsSec, transitionSec, output.XfadeTransition);

        args.Add("-filter_complex");
        args.Add(filter);
        args.Add("-map");
        args.Add("[vout]");
        args.AddRange(FfmpegVideoEncodeArgs.Build(output.VideoCodec, output.VideoPreset, output.VideoCrf));
        args.AddRange([
            "-r", output.FrameRate.ToString(CultureInfo.InvariantCulture),
            "-t", totalDurationSeconds.ToString("F3", CultureInfo.InvariantCulture),
            "-an",
            outputPath
        ]);

        return args;
    }

    public IReadOnlyList<string> BuildConcatDemuxerCommand(string concatListPath, string outputPath) =>
    [
        "-hide_banner", "-y",
        "-f", "concat",
        "-safe", "0",
        "-i", concatListPath,
        "-c", "copy",
        outputPath
    ];

    public IReadOnlyList<string> BuildFinalMuxCommand(
        string backgroundVideoPath,
        string assFilePath,
        string audioFilePath,
        string outputPath,
        RenderOutputConfiguration output)
    {
        var assFilter = _filterGraphBuilder.BuildAssOverlayFilter(assFilePath);

        var args = new List<string>
        {
            "-hide_banner", "-y",
            "-i", backgroundVideoPath,
            "-i", audioFilePath,
            "-vf", assFilter,
            "-map", "0:v:0",
            "-map", "1:a:0"
        };
        args.AddRange(FfmpegVideoEncodeArgs.Build(output.VideoCodec, output.VideoPreset, output.VideoCrf));
        args.AddRange([
            "-c:a", output.AudioCodec,
            "-b:a", $"{output.AudioBitrateKbps}k",
            "-shortest",
            "-movflags", "+faststart",
            "-f", output.OutputFormat,
            outputPath
        ]);
        return args;
    }
}
