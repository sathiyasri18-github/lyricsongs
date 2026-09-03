using Lyricsongs.Application.Rendering;

namespace Lyricsongs.Infrastructure.Rendering;

public sealed class TemporaryRenderDirectory : ITemporaryRenderDirectory
{
    private bool _disposed;

    private TemporaryRenderDirectory(string path)
    {
        Path = path;
    }

    public string Path { get; }

    public static async Task<TemporaryRenderDirectory> CreateAsync(string jobId, string? basePath, CancellationToken cancellationToken)
    {
        var root = basePath ?? System.IO.Path.Combine(System.IO.Path.GetTempPath(), "lyricsongs");
        Directory.CreateDirectory(root);

        var directory = System.IO.Path.Combine(root, $"render_{jobId}_{Guid.NewGuid():N}");
        await Task.Run(() => Directory.CreateDirectory(directory), cancellationToken);
        return new TemporaryRenderDirectory(directory);
    }

    public string CreateFile(string fileName)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return System.IO.Path.Combine(Path, fileName);
    }

    public Task CleanupAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            return Task.CompletedTask;
        }

        return Task.Run(() =>
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        await CleanupAsync();
    }
}

public sealed class TemporaryRenderDirectoryFactory : ITemporaryRenderDirectoryFactory
{
    public async Task<ITemporaryRenderDirectory> CreateAsync(
        string jobId,
        string? basePath = null,
        CancellationToken cancellationToken = default) =>
        await TemporaryRenderDirectory.CreateAsync(jobId, basePath, cancellationToken);
}
