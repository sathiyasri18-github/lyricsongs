using Lyricsongs.Application.Jobs;
using Lyricsongs.Application.Lyrics;
using Lyricsongs.Application.Projects;
using Lyricsongs.Application.Rendering;
using Lyricsongs.Application.Subtitles;
using Lyricsongs.Application.Timeline;
using Lyricsongs.Application.VideoPlan;
using Lyricsongs.Infrastructure.Jobs;
using Lyricsongs.Infrastructure.Lyrics;
using Lyricsongs.Infrastructure.Lyrics.Renderers;
using Lyricsongs.Infrastructure.Projects;
using Lyricsongs.Infrastructure.Rendering;
using Lyricsongs.Infrastructure.Storage;
using Lyricsongs.Infrastructure.Subtitles;
using Lyricsongs.Infrastructure.Timeline;
using Lyricsongs.Infrastructure.VideoPlan;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyricsongs.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<ISrtParser, SrtParser>();
        services.AddSingleton<ISrtValidationService, SrtValidationService>();
        services.AddScoped<ISrtProcessingService, SrtProcessingService>();

        services.AddSingleton<IBackgroundTimelineValidationService, BackgroundTimelineValidationService>();
        services.AddScoped<IBackgroundTimelineService, BackgroundTimelineService>();

        services.AddSingleton<ILyricStyleConfigurationLoader, LyricStyleConfigurationLoader>();
        foreach (var renderer in LyricStyleRendererRegistration.CreateAllRenderers())
        {
            services.AddSingleton<ILyricStyleRenderer>(renderer);
        }
        services.AddSingleton<ILyricStyleRendererFactory, LyricStyleRendererFactory>();
        services.AddScoped<ILyricAnimationEngine, LyricAnimationEngine>();

        services.AddSingleton<IBackgroundPlanner, BackgroundPlanner>();
        services.AddSingleton<ILyricStyleManager, LyricStyleManager>();
        services.AddSingleton<IVideoPlanValidator, VideoPlanValidator>();
        services.AddScoped<IVideoPlanEngine, VideoPlanEngine>();

        services.AddOptions<FfmpegOptions>()
            .BindConfiguration("Ffmpeg")
            .PostConfigure(FfmpegExecutableResolver.Resolve)
            .PostConfigure<ILoggerFactory>((options, loggerFactory) =>
                FfmpegHardwareEncoderSelector.Select(
                    options,
                    loggerFactory.CreateLogger("Lyricsongs.Infrastructure.Rendering.FfmpegHardwareEncoderSelector")));

        services.AddSingleton<IFfmpegProgressParser, FfmpegProgressParser>();
        services.AddSingleton<IFfmpegFilterGraphBuilder, FfmpegFilterGraphBuilder>();
        services.AddSingleton<IFfmpegCommandBuilder, FfmpegCommandBuilder>();
        services.AddSingleton<IFfmpegProcessRunner, FfmpegProcessRunner>();
        services.AddSingleton<IRenderPipelinePlanner, RenderPipelinePlanner>();
        services.AddSingleton<ITemporaryRenderDirectoryFactory, TemporaryRenderDirectoryFactory>();
        services.AddSingleton<IFfmpegHealthCheck, FfmpegHealthCheck>();
        services.AddSingleton<IMediaProbeService, FfmpegMediaProbeService>();
        services.AddScoped<IVideoRenderEngine, VideoRenderEngine>();

        services.AddScoped<IProjectPlanService, ProjectPlanService>();

        services.AddSingleton<IProjectFileStorage, ProjectFileStorage>();
        services.AddSingleton<IProjectStore, FileProjectStore>();
        services.AddSingleton<IRenderJobStore, FileRenderJobStore>();
        services.AddSingleton<IRenderJobQueue, ChannelRenderJobQueue>();
        services.AddSingleton<IRenderJobCancellationRegistry, RenderJobCancellationRegistry>();
        services.AddSingleton<IProjectRenderValidator, ProjectRenderValidator>();
        services.AddSingleton<IRenderJobRecovery, RenderJobRecoveryService>();
        services.AddScoped<IRenderJobService, RenderJobService>();
        services.AddHostedService<RenderWorkerService>();

        return services;
    }
}
