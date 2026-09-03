using System.Diagnostics;
using System.Globalization;
using System.Text;
using Lyricsongs.Application.Rendering;
using Lyricsongs.Domain.Rendering;
using Microsoft.Extensions.Logging;

namespace Lyricsongs.Infrastructure.Rendering;

/// <summary>
/// Multi-stage FFmpeg rendering pipeline:
/// 1. Write ASS subtitles
/// 2. Render each background segment (Ken Burns / trim / loop)
/// 3. Composite segments with xfade transitions
/// 4. Overlay lyrics and mux MP3 audio into final MP4
/// </summary>
public sealed class VideoRenderEngine : IVideoRenderEngine
{
    private readonly IRenderPipelinePlanner _planner;
    private readonly IFfmpegCommandBuilder _commandBuilder;
    private readonly IFfmpegFilterGraphBuilder _filterGraphBuilder;
    private readonly IFfmpegProcessRunner _processRunner;
    private readonly ITemporaryRenderDirectoryFactory _tempDirectoryFactory;
    private readonly ILogger<VideoRenderEngine> _logger;

    private const double SegmentStageWeight = 0.55;
    private const double CompositeStageWeight = 0.18;
    private const double FinalStageWeight = 0.25;

    public VideoRenderEngine(
        IRenderPipelinePlanner planner,
        IFfmpegCommandBuilder commandBuilder,
        IFfmpegFilterGraphBuilder filterGraphBuilder,
        IFfmpegProcessRunner processRunner,
        ITemporaryRenderDirectoryFactory tempDirectoryFactory,
        ILogger<VideoRenderEngine> logger)
    {
        _planner = planner;
        _commandBuilder = commandBuilder;
        _filterGraphBuilder = filterGraphBuilder;
        _processRunner = processRunner;
        _tempDirectoryFactory = tempDirectoryFactory;
        _logger = logger;
    }

    public async Task<RenderResult> RenderAsync(
        VideoRenderRequest request,
        IProgress<RenderProgressReport>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var stopwatch = Stopwatch.StartNew();
        var executedCommands = new List<string>();
        ITemporaryRenderDirectory? tempDir = null;

        try
        {
            Report(progress, request.JobId, RenderStage.Preparing, 0, 0, request.Timeline.AudioDurationMs, "Preparing render workspace");

            tempDir = await _tempDirectoryFactory.CreateAsync(
                request.JobId,
                request.WorkingDirectory,
                cancellationToken);

            var plan = _planner.Plan(request, tempDir.Path);

            Report(progress, request.JobId, RenderStage.WritingSubtitles, 2, 100, plan.TotalDurationMs, "Writing ASS subtitle file");
            await WriteAssFileAsync(plan.AssFilePath, request.LyricPlan.AssContent, cancellationToken);
            executedCommands.Add($"# write {plan.AssFilePath}");

            await RenderSegmentsAsync(request, plan, executedCommands, progress, cancellationToken);

            await CompositeBackgroundAsync(request, plan, executedCommands, progress, cancellationToken);

            await FinalMuxAsync(request, plan, executedCommands, progress, cancellationToken);

            var finalOutput = await FinalizeOutputAsync(plan, request, cancellationToken);

            Report(progress, request.JobId, RenderStage.Completed, 100, 100, plan.TotalDurationMs, "Render completed");

            stopwatch.Stop();
            return new RenderResult
            {
                JobId = request.JobId,
                Success = true,
                OutputFilePath = finalOutput,
                FinalStage = RenderStage.Completed,
                ExecutedCommands = executedCommands,
                IntermediateFiles = CollectIntermediateFiles(plan),
                Elapsed = stopwatch.Elapsed
            };
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            Report(progress, request.JobId, RenderStage.Cancelled, 0, 0, request.Timeline.AudioDurationMs, "Render cancelled");
            return new RenderResult
            {
                JobId = request.JobId,
                Success = false,
                FinalStage = RenderStage.Cancelled,
                ErrorMessage = "Render was cancelled.",
                ExecutedCommands = executedCommands,
                Elapsed = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Render job {JobId} failed", request.JobId);
            stopwatch.Stop();
            Report(progress, request.JobId, RenderStage.Failed, 0, 0, request.Timeline.AudioDurationMs, ex.Message);

            return new RenderResult
            {
                JobId = request.JobId,
                Success = false,
                FinalStage = RenderStage.Failed,
                ErrorMessage = ex.Message,
                ExecutedCommands = executedCommands,
                Elapsed = stopwatch.Elapsed
            };
        }
        finally
        {
            if (tempDir is not null)
            {
                await tempDir.DisposeAsync();
            }
        }
    }

    private async Task RenderSegmentsAsync(
        VideoRenderRequest request,
        RenderPipelinePlan plan,
        List<string> executedCommands,
        IProgress<RenderProgressReport>? progress,
        CancellationToken cancellationToken)
    {
        var segments = plan.Segments;
        for (var i = 0; i < segments.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var segment = segments[i];

            Report(progress, request.JobId, RenderStage.RenderingSegments,
                2 + ((i / (double)segments.Count) * SegmentStageWeight * 100),
                (i / (double)segments.Count) * 100,
                plan.TotalDurationMs,
                $"Rendering segment {segment.Sequence}/{segments.Count}");

            var args = segment.IsImage
                ? _commandBuilder.BuildSegmentImageCommand(segment, request.Output)
                : _commandBuilder.BuildSegmentVideoCommand(segment, request.Output);

            var segmentDurationMs = (long)(segment.DurationSeconds * 1000);
            var result = await _processRunner.RunAsync(
                args,
                segmentDurationMs,
                CreateStageProgress(progress, request.JobId, RenderStage.RenderingSegments,
                    2 + (i / (double)segments.Count) * SegmentStageWeight * 100,
                    SegmentStageWeight * 100 / segments.Count,
                    plan.TotalDurationMs),
                cancellationToken);

            executedCommands.Add(result.CommandLine);
            EnsureSuccess(result, $"Segment {segment.Sequence} render failed");
        }
    }

    private async Task CompositeBackgroundAsync(
        VideoRenderRequest request,
        RenderPipelinePlan plan,
        List<string> executedCommands,
        IProgress<RenderProgressReport>? progress,
        CancellationToken cancellationToken)
    {
        Report(progress, request.JobId, RenderStage.CompositingBackground,
            (2 + SegmentStageWeight * 100),
            0,
            plan.TotalDurationMs,
            "Compositing background with transitions");

        var segmentPaths = plan.Segments.Select(s => s.OutputSegmentPath).ToList();
        var segmentDurations = plan.Segments.Select(s => s.DurationSeconds).ToList();
        var transitions = plan.Segments.Select(s => s.TransitionOutDurationMs).ToList();
        var totalSec = plan.TotalDurationMs / 1000.0;

        IReadOnlyList<string> args;

        var transitionValues = transitions.Take(segmentPaths.Count - 1).ToList();
        var useXfade = segmentPaths.Count > 1 && transitionValues.Any(t => t > 0);

        if (useXfade)
        {
            var filterScriptPath = System.IO.Path.Combine(plan.WorkingDirectory, "xfade_filter.txt");
            var transitionSec = transitionValues.Select(ms => ms / 1000.0).ToList();
            var filter = _filterGraphBuilder.BuildXfadeFilterComplex(segmentDurations, transitionSec, request.Output.XfadeTransition);
            await File.WriteAllTextAsync(filterScriptPath, filter, cancellationToken);
            executedCommands.Add($"# filter script {filterScriptPath}: {filter}");

            args = BuildXfadeWithScriptCommand(segmentPaths, filterScriptPath, totalSec, plan.BackgroundVideoPath, request.Output);
        }
        else if (segmentPaths.Count > 1)
        {
            var concatListPath = System.IO.Path.Combine(plan.WorkingDirectory, "concat_list.txt");
            var concatContent = string.Join(Environment.NewLine,
                segmentPaths.Select(p => $"file '{p.Replace("'", "'\\''")}'"));
            await File.WriteAllTextAsync(concatListPath, concatContent, cancellationToken);
            executedCommands.Add($"# concat list {concatListPath}");

            args = _commandBuilder.BuildConcatDemuxerCommand(concatListPath, plan.BackgroundVideoPath);
        }
        else
        {
            args = _commandBuilder.BuildXfadeConcatCommand(
                segmentPaths,
                segmentDurations,
                transitions,
                totalSec,
                plan.BackgroundVideoPath,
                request.Output);
        }

        var result = await _processRunner.RunAsync(
            args,
            plan.TotalDurationMs,
            CreateStageProgress(progress, request.JobId, RenderStage.CompositingBackground,
                (2 + SegmentStageWeight * 100), CompositeStageWeight * 100, plan.TotalDurationMs),
            cancellationToken);

        executedCommands.Add(result.CommandLine);
        EnsureSuccess(result, "Background composition failed");
    }

    private static IReadOnlyList<string> BuildXfadeWithScriptCommand(
        IReadOnlyList<string> segmentPaths,
        string filterScriptPath,
        double totalDurationSeconds,
        string outputPath,
        RenderOutputConfiguration output)
    {
        var args = new List<string> { "-hide_banner", "-y" };
        foreach (var path in segmentPaths)
        {
            args.Add("-i");
            args.Add(path);
        }

        args.Add("-filter_complex_script");
        args.Add(filterScriptPath);
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

    private async Task FinalMuxAsync(
        VideoRenderRequest request,
        RenderPipelinePlan plan,
        List<string> executedCommands,
        IProgress<RenderProgressReport>? progress,
        CancellationToken cancellationToken)
    {
        Report(progress, request.JobId, RenderStage.OverlayingLyrics,
            (2 + (SegmentStageWeight + CompositeStageWeight) * 100),
            0,
            plan.TotalDurationMs,
            "Overlaying lyrics and muxing audio");

        var args = _commandBuilder.BuildFinalMuxCommand(
            plan.BackgroundVideoPath,
            plan.AssFilePath,
            request.AudioFilePath,
            plan.OutputFilePath,
            request.Output);

        var result = await _processRunner.RunAsync(
            args,
            plan.TotalDurationMs,
            CreateStageProgress(progress, request.JobId, RenderStage.MuxingAudio,
                (2 + (SegmentStageWeight + CompositeStageWeight) * 100),
                FinalStageWeight * 100,
                plan.TotalDurationMs),
            cancellationToken);

        executedCommands.Add(result.CommandLine);
        EnsureSuccess(result, "Final mux failed");
    }

    private static async Task<string> FinalizeOutputAsync(
        RenderPipelinePlan plan,
        VideoRenderRequest request,
        CancellationToken cancellationToken)
    {
        if (request.OutputFilePath is not null &&
            !string.Equals(request.OutputFilePath, plan.OutputFilePath, StringComparison.OrdinalIgnoreCase))
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(request.OutputFilePath)!);
            File.Copy(plan.OutputFilePath, request.OutputFilePath, overwrite: true);
            return request.OutputFilePath;
        }

        if (!File.Exists(plan.OutputFilePath))
        {
            throw new FfmpegRenderException("Expected output file was not created.", null, -1, null);
        }

        return plan.OutputFilePath;
    }

    private static async Task WriteAssFileAsync(string path, string content, CancellationToken cancellationToken)
    {
        // UTF-8 with BOM helps libass on Windows locate glyphs for Tamil fonts.
        var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        await File.WriteAllTextAsync(path, content, encoding, cancellationToken);
    }

    private static void EnsureSuccess(FfmpegExecutionResult result, string message)
    {
        if (result.IsSuccess)
        {
            return;
        }

        var detail = TrimTail(result.StandardError, 4000);
        throw new FfmpegRenderException(
            $"{message} (exit {result.ExitCode}). {detail}",
            result.CommandLine,
            result.ExitCode,
            result.StandardError);
    }

    private static string TrimTail(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[^maxLength..];

    private static IProgress<FfmpegProgressUpdate>? CreateStageProgress(
        IProgress<RenderProgressReport>? progress,
        string jobId,
        RenderStage stage,
        double stageStartPercent,
        double stageSpanPercent,
        long totalDurationMs) =>
        progress is null ? null : new Progress<FfmpegProgressUpdate>(update =>
        {
            if (update.ProcessedDurationMs is null || totalDurationMs <= 0)
            {
                return;
            }

            var stagePct = Math.Clamp(update.ProcessedDurationMs.Value / (double)totalDurationMs * 100, 0, 100);
            var overall = stageStartPercent + (stagePct / 100.0) * stageSpanPercent;

            progress.Report(new RenderProgressReport
            {
                JobId = jobId,
                Stage = stage,
                OverallPercent = Math.Min(99, overall),
                StagePercent = stagePct,
                ProcessedDurationMs = update.ProcessedDurationMs,
                TotalDurationMs = totalDurationMs
            });
        });

    private static void Report(
        IProgress<RenderProgressReport>? progress,
        string jobId,
        RenderStage stage,
        double overallPercent,
        double stagePercent,
        long totalDurationMs,
        string? message)
    {
        progress?.Report(new RenderProgressReport
        {
            JobId = jobId,
            Stage = stage,
            OverallPercent = overallPercent,
            StagePercent = stagePercent,
            TotalDurationMs = totalDurationMs,
            Message = message
        });
    }

    private static IReadOnlyList<string> CollectIntermediateFiles(RenderPipelinePlan plan)
    {
        var files = plan.Segments.Select(s => s.OutputSegmentPath).ToList();
        files.Add(plan.AssFilePath);
        files.Add(plan.BackgroundVideoPath);
        files.Add(plan.OutputFilePath);
        return files;
    }
}
