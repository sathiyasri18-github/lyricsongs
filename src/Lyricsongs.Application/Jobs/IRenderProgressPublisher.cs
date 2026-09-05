namespace Lyricsongs.Application.Jobs;

public interface IRenderProgressPublisher
{
    Task PublishStartedAsync(
        string projectId,
        string jobId,
        string status,
        double progress,
        string? message = null);

    Task PublishProgressAsync(
        string projectId,
        string jobId,
        string status,
        double progress,
        string? message = null);

    Task PublishCompletedAsync(string projectId, string jobId, string downloadUrl);

    Task PublishFailedAsync(string projectId, string jobId, string status, string error);
}
