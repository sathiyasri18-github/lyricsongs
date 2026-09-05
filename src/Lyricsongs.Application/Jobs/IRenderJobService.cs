using Lyricsongs.Domain.VideoPlan;

namespace Lyricsongs.Application.Jobs;

public interface IRenderJobService
{
    Task<RenderJobDto> EnqueueAsync(
        string projectId,
        VideoPlanSettings? settings = null,
        CancellationToken cancellationToken = default);

    Task<RenderJobDto?> GetAsync(string jobId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RenderJobDto>> ListAsync(
        string? projectId = null,
        CancellationToken cancellationToken = default);

    Task<bool> CancelAsync(string jobId, CancellationToken cancellationToken = default);
}

public sealed class RenderJobDto
{
    public required string JobId { get; init; }
    public required string ProjectId { get; init; }
    public required string ProjectName { get; init; }
    public required string Status { get; init; }
    public double Progress { get; init; }
    public string? Message { get; init; }
    public string? ErrorMessage { get; init; }
    public string? OutputUrl { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
}

public sealed class CreateRenderJobRequest
{
    public VideoPlanSettings? Settings { get; init; }
}
