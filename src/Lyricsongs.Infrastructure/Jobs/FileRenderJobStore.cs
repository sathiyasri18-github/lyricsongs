using System.Collections.Concurrent;
using Lyricsongs.Application.Jobs;
using Lyricsongs.Domain.Jobs;
using Lyricsongs.Infrastructure.Storage;

namespace Lyricsongs.Infrastructure.Jobs;

public sealed class FileRenderJobStore : IRenderJobStore
{
    private readonly IProjectFileStorage _storage;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new(StringComparer.Ordinal);

    public FileRenderJobStore(IProjectFileStorage storage)
    {
        _storage = storage;
    }

    public async Task SaveAsync(RenderJobRecord job, CancellationToken cancellationToken = default)
    {
        var gate = _locks.GetOrAdd(job.Id, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            job.UpdatedAt = DateTimeOffset.UtcNow;
            await FileJsonStore.WriteAsync(GetJobFilePath(job.Id), job, cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<RenderJobRecord?> GetAsync(string jobId, CancellationToken cancellationToken = default)
    {
        var gate = _locks.GetOrAdd(jobId, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            return await FileJsonStore.ReadAsync<RenderJobRecord>(GetJobFilePath(jobId), cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<IReadOnlyList<RenderJobRecord>> ListAsync(
        string? projectId = null,
        CancellationToken cancellationToken = default)
    {
        var jobsRoot = Path.Combine(_storage.RootPath, "jobs");
        if (!Directory.Exists(jobsRoot))
        {
            return [];
        }

        var jobs = new List<RenderJobRecord>();
        foreach (var file in Directory.EnumerateFiles(jobsRoot, "*.json"))
        {
            var jobId = Path.GetFileNameWithoutExtension(file);
            var job = await GetAsync(jobId, cancellationToken);
            if (job is null)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(projectId) &&
                !string.Equals(job.ProjectId, projectId, StringComparison.Ordinal))
            {
                continue;
            }

            jobs.Add(job);
        }

        return jobs.OrderByDescending(j => j.CreatedAt).ToList();
    }

    public async Task DeleteByProjectIdAsync(string projectId, CancellationToken cancellationToken = default)
    {
        var jobs = await ListAsync(projectId, cancellationToken);
        foreach (var job in jobs)
        {
            var path = GetJobFilePath(job.Id);
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            _locks.TryRemove(job.Id, out _);
        }
    }

    private string GetJobFilePath(string jobId) =>
        Path.Combine(_storage.RootPath, "jobs", $"{jobId}.json");
}
