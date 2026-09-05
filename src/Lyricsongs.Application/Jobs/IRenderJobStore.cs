using Lyricsongs.Domain.Jobs;

namespace Lyricsongs.Application.Jobs;

public interface IRenderJobStore
{
    Task SaveAsync(RenderJobRecord job, CancellationToken cancellationToken = default);
    Task<RenderJobRecord?> GetAsync(string jobId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RenderJobRecord>> ListAsync(string? projectId = null, CancellationToken cancellationToken = default);
    Task DeleteByProjectIdAsync(string projectId, CancellationToken cancellationToken = default);
}
