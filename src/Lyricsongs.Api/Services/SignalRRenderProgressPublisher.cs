using Lyricsongs.Api.Hubs;
using Lyricsongs.Application.Jobs;
using Microsoft.AspNetCore.SignalR;

namespace Lyricsongs.Api.Services;

public sealed class SignalRRenderProgressPublisher : IRenderProgressPublisher
{
    private readonly IHubContext<RenderProgressHub> _hub;

    public SignalRRenderProgressPublisher(IHubContext<RenderProgressHub> hub)
    {
        _hub = hub;
    }

    public Task PublishStartedAsync(
        string projectId,
        string jobId,
        string status,
        double progress,
        string? message = null) =>
        SendAsync(projectId, "RenderStarted", jobId, status, progress, message);

    public Task PublishProgressAsync(
        string projectId,
        string jobId,
        string status,
        double progress,
        string? message = null) =>
        SendAsync(projectId, "RenderProgress", jobId, status, progress, message);

    public Task PublishCompletedAsync(string projectId, string jobId, string downloadUrl) =>
        _hub.Clients.Group(RenderProgressHub.ProjectGroup(projectId)).SendAsync(
            "RenderCompleted",
            new
            {
                jobId,
                status = "Completed",
                progress = 100,
                message = "Render completed",
                downloadUrl
            });

    public Task PublishFailedAsync(string projectId, string jobId, string status, string error) =>
        _hub.Clients.Group(RenderProgressHub.ProjectGroup(projectId)).SendAsync(
            "RenderFailed",
            new
            {
                jobId,
                status,
                progress = 0,
                message = error,
                error
            });

    private Task SendAsync(
        string projectId,
        string eventName,
        string jobId,
        string status,
        double progress,
        string? message) =>
        _hub.Clients.Group(RenderProgressHub.ProjectGroup(projectId)).SendAsync(
            eventName,
            new
            {
                jobId,
                status,
                progress = Math.Round(progress, 1),
                message
            });
}
