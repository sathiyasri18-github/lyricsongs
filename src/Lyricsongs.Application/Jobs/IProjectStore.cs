using Lyricsongs.Domain.Jobs;

namespace Lyricsongs.Application.Jobs;

public interface IProjectStore
{
    Task<ProjectRecord> CreateAsync(string name, CancellationToken cancellationToken = default);
    Task<ProjectRecord?> GetAsync(string projectId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectRecord>> ListAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(ProjectRecord project, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string projectId, CancellationToken cancellationToken = default);
}
