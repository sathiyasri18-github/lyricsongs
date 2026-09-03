using System.Collections.Concurrent;
using Lyricsongs.Application.Jobs;
using Lyricsongs.Domain.Jobs;
using Lyricsongs.Infrastructure.Storage;
using Microsoft.Extensions.Logging;

namespace Lyricsongs.Infrastructure.Jobs;

public sealed class FileProjectStore : IProjectStore
{
    private readonly IProjectFileStorage _storage;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new(StringComparer.Ordinal);

    public FileProjectStore(IProjectFileStorage storage)
    {
        _storage = storage;
    }

    public async Task<ProjectRecord> CreateAsync(string name, CancellationToken cancellationToken = default)
    {
        var project = new ProjectRecord
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = string.IsNullOrWhiteSpace(name) ? "Untitled Project" : name.Trim()
        };

        Directory.CreateDirectory(GetProjectDirectory(project.Id));
        await SaveAsync(project, cancellationToken);
        return project;
    }

    public async Task<ProjectRecord?> GetAsync(string projectId, CancellationToken cancellationToken = default)
    {
        var gate = _locks.GetOrAdd(projectId, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            return await FileJsonStore.ReadAsync<ProjectRecord>(GetProjectFilePath(projectId), cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<IReadOnlyList<ProjectRecord>> ListAsync(CancellationToken cancellationToken = default)
    {
        var projectsRoot = Path.Combine(_storage.RootPath, "projects");
        if (!Directory.Exists(projectsRoot))
        {
            return [];
        }

        var projects = new List<ProjectRecord>();
        foreach (var dir in Directory.EnumerateDirectories(projectsRoot))
        {
            var projectId = Path.GetFileName(dir);
            var project = await GetAsync(projectId, cancellationToken);
            if (project is not null)
            {
                projects.Add(project);
            }
        }

        return projects.OrderByDescending(p => p.UpdatedAt).ToList();
    }

    public async Task SaveAsync(ProjectRecord project, CancellationToken cancellationToken = default)
    {
        var gate = _locks.GetOrAdd(project.Id, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            project.UpdatedAt = DateTimeOffset.UtcNow;
            Directory.CreateDirectory(GetProjectDirectory(project.Id));
            await FileJsonStore.WriteAsync(GetProjectFilePath(project.Id), project, cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<bool> DeleteAsync(string projectId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(projectId))
        {
            return false;
        }

        var gate = _locks.GetOrAdd(projectId, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            var directory = GetProjectDirectory(projectId);
            if (!Directory.Exists(directory))
            {
                return false;
            }

            Directory.Delete(directory, recursive: true);
            _locks.TryRemove(projectId, out _);
            return true;
        }
        finally
        {
            gate.Release();
        }
    }

    private string GetProjectDirectory(string projectId) =>
        Path.Combine(_storage.RootPath, "projects", projectId);

    private string GetProjectFilePath(string projectId) =>
        Path.Combine(GetProjectDirectory(projectId), "project.json");
}
