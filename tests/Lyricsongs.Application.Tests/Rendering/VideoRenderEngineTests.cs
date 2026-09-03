using Lyricsongs.Application.Rendering;
using Lyricsongs.Domain.Lyrics;
using Lyricsongs.Domain.Rendering;
using Lyricsongs.Domain.Timeline;
using Lyricsongs.Infrastructure.Rendering;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyricsongs.Application.Tests.Rendering;

public class VideoRenderEngineTests
{
    [Fact]
    public async Task RenderAsync_RunsAllPipelineStages()
    {
        var runner = new RecordingFfmpegProcessRunner();
        var engine = CreateEngine(runner);
        var request = RenderTestData.CreateRequest();

        var result = await engine.RenderAsync(request);

        Assert.True(result.Success);
        Assert.Equal(RenderStage.Completed, result.FinalStage);
        Assert.True(result.ExecutedCommands.Count >= 3);
        Assert.Contains(runner.Commands, c => c.Any(a => a.Contains("zoompan")));
        Assert.Contains(runner.Commands, c => c.Any(a => a == "concat"));
        Assert.Contains(runner.Commands, c => string.Join(' ', c).Contains("ass="));
    }

    [Fact]
    public async Task RenderAsync_Cancellation_ReturnsCancelled()
    {
        var runner = new RecordingFfmpegProcessRunner(delayMs: 500);
        var engine = CreateEngine(runner);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await engine.RenderAsync(RenderTestData.CreateRequest(), cancellationToken: cts.Token);

        Assert.False(result.Success);
        Assert.Equal(RenderStage.Cancelled, result.FinalStage);
    }

    [Fact]
    public async Task RenderAsync_FfmpegFailure_ReturnsFailed()
    {
        var runner = new RecordingFfmpegProcessRunner(failOnInvocation: 1);
        var engine = CreateEngine(runner);
        var result = await engine.RenderAsync(RenderTestData.CreateRequest());

        Assert.False(result.Success);
        Assert.Equal(RenderStage.Failed, result.FinalStage);
        Assert.Contains("Segment", result.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
    }

    private static VideoRenderEngine CreateEngine(RecordingFfmpegProcessRunner runner)
    {
        return new VideoRenderEngine(
            new RenderPipelinePlanner(),
            new FfmpegCommandBuilder(new FfmpegFilterGraphBuilder()),
            new FfmpegFilterGraphBuilder(),
            runner,
            new TemporaryRenderDirectoryFactory(),
            NullLogger<VideoRenderEngine>.Instance);
    }
}

internal sealed class RecordingFfmpegProcessRunner : IFfmpegProcessRunner
{
    private readonly int _failOnInvocation;
    private readonly int _delayMs;
    private int _invocation;

    public RecordingFfmpegProcessRunner(int failOnInvocation = -1, int delayMs = 0)
    {
        _failOnInvocation = failOnInvocation;
        _delayMs = delayMs;
    }

    public List<IReadOnlyList<string>> Commands { get; } = [];

    public async Task<FfmpegExecutionResult> RunAsync(
        IReadOnlyList<string> arguments,
        long? totalDurationMs = null,
        IProgress<FfmpegProgressUpdate>? progress = null,
        CancellationToken cancellationToken = default)
    {
        _invocation++;
        Commands.Add(arguments.ToList());

        cancellationToken.ThrowIfCancellationRequested();

        if (_delayMs > 0)
        {
            await Task.Delay(_delayMs, cancellationToken);
        }

        // Simulate creating expected output files for copy/mux stages
        var outputArg = arguments.LastOrDefault();
        if (outputArg is not null && (outputArg.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase)))
        {
            var dir = System.IO.Path.GetDirectoryName(outputArg);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            await File.WriteAllTextAsync(outputArg, "mock", cancellationToken);
        }

        progress?.Report(new FfmpegProgressUpdate { ProcessedDurationMs = totalDurationMs ?? 1000 });

        var exitCode = _invocation == _failOnInvocation ? 1 : 0;

        return new FfmpegExecutionResult
        {
            ExitCode = exitCode,
            Arguments = arguments.ToList(),
            CommandLine = FfmpegPathHelper.ToCommandLine("ffmpeg", arguments),
            StandardError = exitCode == 0 ? string.Empty : "mock ffmpeg error",
            StandardOutput = string.Empty,
            Elapsed = TimeSpan.FromMilliseconds(1)
        };
    }
}

internal static class RenderTestData
{
    public static VideoRenderRequest CreateRequest()
    {
        var timeline = new BackgroundTimeline
        {
            AudioDurationMs = 10_000,
            AudioDuration = "00:00:10.000",
            AvailableVideoDurationMs = 5000,
            TransitionDurationMs = 0,
            OrderingMode = AssetOrderingMode.Manual,
            OrderingSeed = 42,
            OrderedAssetIds = ["bg1", "clip1"],
            Segments =
            [
                new TimelineSegment
                {
                    Sequence = 1,
                    AssetId = "bg1",
                    FileName = "bg1.jpg",
                    AssetType = BackgroundAssetType.Image,
                    StartMs = 0,
                    EndMs = 5000,
                    Start = "00:00:00.000",
                    End = "00:00:05.000",
                    SlotDurationMs = 5000,
                    TransitionOutDurationMs = 0
                },
                new TimelineSegment
                {
                    Sequence = 2,
                    AssetId = "clip1",
                    FileName = "clip1.mp4",
                    AssetType = BackgroundAssetType.Video,
                    StartMs = 5000,
                    EndMs = 10000,
                    Start = "00:00:05.000",
                    End = "00:00:10.000",
                    SlotDurationMs = 5000,
                    TransitionOutDurationMs = 0,
                    LoopCount = 1,
                    SourceDurationMs = 5000
                }
            ],
            Summary = new BackgroundTimelineSummary
            {
                SegmentCount = 2,
                ImageCount = 1,
                VideoCount = 1,
                TotalSlotDurationMs = 10000,
                TotalTransitionOverlapMs = 0,
                TimelineEndMs = 10000
            }
        };

        return new VideoRenderRequest
        {
            JobId = "test-job",
            AudioFilePath = CreateTempFile("audio.mp3"),
            Timeline = timeline,
            AssetPathMap = new Dictionary<string, string>
            {
                ["bg1"] = CreateTempFile("bg1.jpg"),
                ["clip1"] = CreateTempFile("clip1.mp4")
            },
            LyricPlan = new LyricRenderPlan
            {
                Style = LyricAnimationStyle.Classic,
                Configuration = new LyricStyleConfiguration(),
                AssContent = "[Script Info]\nTitle: Test\n\n[Events]\n",
                Ffmpeg = new LyricFfmpegFilterPlan
                {
                    SubtitleFilter = "ass='lyrics.ass'",
                    FilterSteps = [],
                    Description = "test"
                },
                StyledCues = []
            },
            Output = new RenderOutputConfiguration()
        };
    }

    private static string CreateTempFile(string name)
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"lyricsongs_test_{Guid.NewGuid():N}_{name}");
        File.WriteAllText(path, "test");
        return path;
    }
}
