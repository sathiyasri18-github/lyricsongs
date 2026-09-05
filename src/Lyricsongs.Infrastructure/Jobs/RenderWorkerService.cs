using Lyricsongs.Application.Jobs;
using Lyricsongs.Application.Lyrics;
using Lyricsongs.Application.Rendering;
using Lyricsongs.Application.Subtitles;
using Lyricsongs.Application.VideoPlan;
using Lyricsongs.Domain.Jobs;
using Lyricsongs.Domain.Lyrics;
using Lyricsongs.Domain.Rendering;
using Lyricsongs.Domain.Subtitles;
using Lyricsongs.Domain.Timeline;
using Lyricsongs.Domain.VideoPlan;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lyricsongs.Infrastructure.Jobs;

public sealed class RenderWorkerService : BackgroundService
{
    private readonly IRenderJobQueue _queue;
    private readonly IRenderJobStore _jobs;
    private readonly IProjectStore _projects;
    private readonly IProjectFileStorage _storage;
    private readonly IProjectRenderValidator _validator;
    private readonly IRenderJobCancellationRegistry _cancellation;
    private readonly IRenderJobRecovery _recovery;
    private readonly IRenderProgressPublisher _progress;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<FfmpegOptions> _ffmpegOptions;
    private readonly ILogger<RenderWorkerService> _logger;

    public RenderWorkerService(
        IRenderJobQueue queue,
        IRenderJobStore jobs,
        IProjectStore projects,
        IProjectFileStorage storage,
        IProjectRenderValidator validator,
        IRenderJobCancellationRegistry cancellation,
        IRenderJobRecovery recovery,
        IRenderProgressPublisher progress,
        IServiceScopeFactory scopeFactory,
        IOptions<FfmpegOptions> ffmpegOptions,
        ILogger<RenderWorkerService> logger)
    {
        _queue = queue;
        _jobs = jobs;
        _projects = projects;
        _storage = storage;
        _validator = validator;
        _cancellation = cancellation;
        _recovery = recovery;
        _progress = progress;
        _scopeFactory = scopeFactory;
        _ffmpegOptions = ffmpegOptions;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await _recovery.RecoverAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Render job recovery failed");
        }

        await foreach (var jobId in _queue.DequeueAllAsync(stoppingToken))
        {
            try
            {
                await ProcessJobAsync(jobId, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error processing render job {JobId}", jobId);
            }
        }
    }

    private async Task ProcessJobAsync(string jobId, CancellationToken stoppingToken)
    {
        var job = await _jobs.GetAsync(jobId, stoppingToken);
        if (job is null)
        {
            return;
        }

        if (job.CancelRequested || job.Status == RenderJobStatus.Cancelled)
        {
            job.Status = RenderJobStatus.Cancelled;
            job.Message = "Cancelled";
            job.CompletedAt = DateTimeOffset.UtcNow;
            await _jobs.SaveAsync(job, stoppingToken);
            await _progress.PublishFailedAsync(job.ProjectId, job.Id, RenderJobStatus.Cancelled.ToString(), "Render cancelled");
            return;
        }

        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            stoppingToken,
            _cancellation.Register(jobId));

        try
        {
            job.Status = RenderJobStatus.Preparing;
            job.StartedAt = DateTimeOffset.UtcNow;
            job.ProgressPercent = 0;
            job.Message = "Preparing render";
            await _jobs.SaveAsync(job, linkedCts.Token);
            await _progress.PublishStartedAsync(
                job.ProjectId, job.Id, job.Status.ToString(), job.ProgressPercent, job.Message);

            var project = await _projects.GetAsync(job.ProjectId, linkedCts.Token)
                ?? throw new InvalidOperationException($"Project '{job.ProjectId}' was not found.");

            var validation = _validator.Validate(project, _storage);
            if (!validation.IsValid)
            {
                throw new InvalidOperationException(string.Join(' ', validation.Errors));
            }

            using var scope = _scopeFactory.CreateScope();
            var srtProcessing = scope.ServiceProvider.GetRequiredService<ISrtProcessingService>();
            var planEngine = scope.ServiceProvider.GetRequiredService<IVideoPlanEngine>();
            var lyricEngine = scope.ServiceProvider.GetRequiredService<ILyricAnimationEngine>();
            var renderEngine = scope.ServiceProvider.GetRequiredService<IVideoRenderEngine>();

            var lyrics = await LoadLyricsAsync(project, srtProcessing, linkedCts.Token);
            var settings = DeserializeSettings(job.SettingsJson ?? project.SettingsJson);
            var assets = project.Assets
                .OrderBy(a => a.SortOrder)
                .Select(a => new VideoPlanAsset
                {
                    AssetId = a.Id,
                    FileName = a.FileName,
                    Type = a.Type.Equals("video", StringComparison.OrdinalIgnoreCase)
                        ? BackgroundAssetType.Video
                        : BackgroundAssetType.Image,
                    SourceDurationMs = a.SourceDurationMs,
                    SortOrder = a.SortOrder
                })
                .ToList();

            var buildResult = planEngine.Build(new VideoPlanRequest
            {
                AudioDurationMs = project.AudioDurationMs
                    ?? throw new InvalidOperationException("Audio duration is required."),
                Lyrics = lyrics,
                Assets = assets,
                Settings = settings
            });

            var plan = buildResult.Plan;
            var lyricConfig = ToLyricConfiguration(plan.Lyrics, plan.Output);
            var lyricPlan = lyricEngine.Render(plan.Subtitles, lyricConfig);
            var timeline = ToBackgroundTimeline(plan, project);
            var outputPath = _storage.GetOutputPath(project.Id, job.Id);
            var workspace = _storage.GetJobWorkspacePath(job.Id);

            job.Status = RenderJobStatus.Rendering;
            job.Message = "Rendering video";
            job.OutputFilePath = outputPath;
            await _jobs.SaveAsync(job, linkedCts.Token);
            await _progress.PublishProgressAsync(
                job.ProjectId, job.Id, job.Status.ToString(), 5, job.Message);

            var request = new VideoRenderRequest
            {
                JobId = job.Id,
                AudioFilePath = _storage.GetAbsolutePath(project.AudioRelativePath!),
                Timeline = timeline,
                AssetPathMap = project.Assets.ToDictionary(
                    a => a.Id,
                    a => _storage.GetAbsolutePath(a.RelativePath),
                    StringComparer.Ordinal),
                LyricPlan = lyricPlan,
                Output = new RenderOutputConfiguration
                {
                    Width = plan.Output.Width,
                    Height = plan.Output.Height,
                    FrameRate = plan.Output.Fps,
                    VideoCodec = string.IsNullOrWhiteSpace(_ffmpegOptions.Value.ResolvedVideoEncoder)
                        ? plan.Output.VideoCodec
                        : _ffmpegOptions.Value.ResolvedVideoEncoder,
                    AudioCodec = plan.Output.AudioCodec,
                    OutputFormat = plan.Output.Format,
                    DefaultKenBurnsMotion = MapKenBurns(plan.Backgrounds.FirstOrDefault()?.Animation),
                    CycleKenBurnsMotions = settings.CycleImageAnimations,
                    XfadeTransition = TransitionCatalog.ToFfmpegName(plan.Transition.Type)
                },
                OutputFilePath = outputPath,
                WorkingDirectory = workspace
            };

            var progress = new Progress<RenderProgressReport>(report =>
            {
                _ = UpdateProgressAsync(job, report);
            });

            var result = await renderEngine.RenderAsync(request, progress, linkedCts.Token);

            job = await _jobs.GetAsync(job.Id, CancellationToken.None) ?? job;

            if (result.Success)
            {
                job.Status = RenderJobStatus.Completed;
                job.ProgressPercent = 100;
                job.Message = "Render completed";
                job.OutputFilePath = result.OutputFilePath ?? outputPath;
                job.ErrorMessage = null;
                job.CompletedAt = DateTimeOffset.UtcNow;
                await _jobs.SaveAsync(job, CancellationToken.None);
                await _progress.PublishCompletedAsync(
                    job.ProjectId,
                    job.Id,
                    _storage.GetDownloadUrlPath(job.ProjectId, job.Id));
            }
            else if (result.FinalStage == RenderStage.Cancelled || job.CancelRequested)
            {
                job.Status = RenderJobStatus.Cancelled;
                job.Message = result.ErrorMessage ?? "Render cancelled";
                job.CompletedAt = DateTimeOffset.UtcNow;
                await _jobs.SaveAsync(job, CancellationToken.None);
                await _progress.PublishFailedAsync(
                    job.ProjectId, job.Id, job.Status.ToString(), job.Message);
            }
            else
            {
                job.Status = RenderJobStatus.Failed;
                job.Message = result.ErrorMessage ?? "Render failed";
                job.ErrorMessage = result.ErrorMessage;
                job.CompletedAt = DateTimeOffset.UtcNow;
                await _jobs.SaveAsync(job, CancellationToken.None);
                await _progress.PublishFailedAsync(
                    job.ProjectId, job.Id, job.Status.ToString(), job.ErrorMessage ?? job.Message);
            }
        }
        catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
        {
            job.Status = RenderJobStatus.Cancelled;
            job.Message = "Render cancelled";
            job.CompletedAt = DateTimeOffset.UtcNow;
            await _jobs.SaveAsync(job, CancellationToken.None);
            await _progress.PublishFailedAsync(
                job.ProjectId, job.Id, job.Status.ToString(), job.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Render job {JobId} failed", jobId);
            job.Status = RenderJobStatus.Failed;
            job.Message = ex.Message;
            job.ErrorMessage = ex.Message;
            job.CompletedAt = DateTimeOffset.UtcNow;
            await _jobs.SaveAsync(job, CancellationToken.None);
            await _progress.PublishFailedAsync(
                job.ProjectId, job.Id, job.Status.ToString(), ex.Message);
        }
        finally
        {
            _cancellation.Complete(jobId);
            linkedCts.Dispose();
            TryCleanupWorkspace(jobId);
        }
    }

    private async Task UpdateProgressAsync(RenderJobRecord job, RenderProgressReport report)
    {
        try
        {
            var latest = await _jobs.GetAsync(job.Id);
            if (latest is null ||
                latest.Status is RenderJobStatus.Completed or RenderJobStatus.Failed or RenderJobStatus.Cancelled)
            {
                return;
            }

            latest.ProgressPercent = Math.Clamp(report.OverallPercent, 0, 99);
            latest.Message = report.Message ?? latest.Message;
            latest.Status = RenderJobStatus.Rendering;
            await _jobs.SaveAsync(latest);
            await _progress.PublishProgressAsync(
                latest.ProjectId,
                latest.Id,
                latest.Status.ToString(),
                latest.ProgressPercent,
                latest.Message);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to publish progress for job {JobId}", job.Id);
        }
    }

    private async Task<IReadOnlyList<LyricCue>> LoadLyricsAsync(
        ProjectRecord project,
        ISrtProcessingService srtProcessing,
        CancellationToken cancellationToken)
    {
        var srtPath = _storage.GetAbsolutePath(project.SrtRelativePath!);
        await using var stream = File.OpenRead(srtPath);
        var result = srtProcessing.Process(stream);
        return result.Lyrics;
    }

    private static VideoPlanSettings DeserializeSettings(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new VideoPlanSettings();
        }

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<VideoPlanSettings>(json)
                   ?? new VideoPlanSettings();
        }
        catch
        {
            return new VideoPlanSettings();
        }
    }

    private static LyricStyleConfiguration ToLyricConfiguration(
        LyricStyleSettings lyrics,
        VideoOutputSettings output) => new()
    {
        Style = lyrics.Style,
        Font = lyrics.Font,
        FontSize = lyrics.FontSize,
        Position = lyrics.Position,
        Alignment = lyrics.Alignment,
        TextColor = lyrics.TextColor,
        TextColor2 = lyrics.TextColor2,
        TextColor3 = lyrics.TextColor3,
        HighlightColor = lyrics.HighlightColor,
        OutlineColor = lyrics.OutlineColor,
        Outline = lyrics.Outline,
        OutlineWidth = lyrics.OutlineWidth,
        Shadow = lyrics.Shadow,
        ShadowColor = lyrics.ShadowColor,
        ShadowDepth = lyrics.ShadowDepth,
        Opacity = lyrics.Opacity,
        FadeInMs = lyrics.FadeInMs,
        FadeOutMs = lyrics.FadeOutMs,
        VideoWidth = output.Width,
        VideoHeight = output.Height
    };

    private static BackgroundTimeline ToBackgroundTimeline(
        Domain.VideoPlan.VideoPlan plan,
        ProjectRecord project)
    {
        var fileNames = project.Assets.ToDictionary(a => a.Id, a => a.FileName, StringComparer.Ordinal);
        var segments = plan.Backgrounds
            .Select((b, index) => new TimelineSegment
            {
                Sequence = index + 1,
                AssetId = b.AssetId,
                FileName = fileNames.GetValueOrDefault(b.AssetId) ?? b.AssetId,
                AssetType = b.Type,
                StartMs = b.StartMs,
                EndMs = b.EndMs,
                Start = new Timecode(b.StartMs).ToDisplayString(),
                End = new Timecode(b.EndMs).ToDisplayString(),
                SlotDurationMs = b.DurationMs + b.TransitionOutDurationMs,
                TransitionOutDurationMs = b.TransitionOutDurationMs,
                PlaybackMode = b.PlaybackMode,
                LoopCount = b.LoopCount,
                TrimStartMs = b.TrimStartMs,
                SourcePlayDurationMs = b.SourcePlayDurationMs,
                SourceDurationMs = b.SourceDurationMs
            })
            .ToList();

        var imageCount = segments.Count(s => s.AssetType == BackgroundAssetType.Image);
        var videoCount = segments.Count - imageCount;

        return new BackgroundTimeline
        {
            AudioDurationMs = plan.AudioDurationMs,
            AudioDuration = new Timecode(plan.AudioDurationMs).ToDisplayString(),
            AvailableVideoDurationMs = segments
                .Where(s => s.AssetType == BackgroundAssetType.Video)
                .Sum(s => s.SourceDurationMs ?? 0),
            TransitionDurationMs = plan.Transition.DurationMs,
            OrderingMode = plan.PlanningMode == BackgroundPlanningMode.Random
                ? AssetOrderingMode.Automatic
                : AssetOrderingMode.Manual,
            OrderingSeed = plan.RandomSeed,
            OrderedAssetIds = plan.OrderedAssetIds,
            Segments = segments,
            Summary = new BackgroundTimelineSummary
            {
                SegmentCount = segments.Count,
                ImageCount = imageCount,
                VideoCount = videoCount,
                TotalSlotDurationMs = segments.Sum(s => s.SlotDurationMs),
                TotalTransitionOverlapMs = segments.Sum(s => s.TransitionOutDurationMs),
                TimelineEndMs = plan.AudioDurationMs
            }
        };
    }

    private static KenBurnsMotion MapKenBurns(BackgroundAnimation? animation) =>
        animation switch
        {
            BackgroundAnimation.ZoomOut or BackgroundAnimation.SlowZoomOut => KenBurnsMotion.ZoomOut,
            BackgroundAnimation.PanLeft => KenBurnsMotion.PanLeft,
            BackgroundAnimation.PanRight => KenBurnsMotion.PanRight,
            _ => KenBurnsMotion.ZoomIn
        };

    private void TryCleanupWorkspace(string jobId)
    {
        try
        {
            var path = _storage.GetJobWorkspacePath(jobId);
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to clean workspace for job {JobId}", jobId);
        }
    }
}
