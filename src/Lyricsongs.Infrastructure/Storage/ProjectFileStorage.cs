using Lyricsongs.Application.Jobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Lyricsongs.Infrastructure.Storage;

public sealed class ProjectFileStorage : IProjectFileStorage
{
    public ProjectFileStorage(IConfiguration configuration, IHostEnvironment environment)
    {
        var configured = configuration["Storage:Root"];
        RootPath = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(environment.ContentRootPath, "storage")
            : Path.GetFullPath(configured);

        Directory.CreateDirectory(RootPath);
    }

    public string RootPath { get; }

    public async Task<string> SaveProjectFileAsync(
        string projectId,
        string folder,
        string fileName,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        var safeName = SanitizeFileName(fileName);
        var relative = Path.Combine("projects", projectId, folder, $"{Guid.NewGuid():N}_{safeName}");
        var absolute = GetAbsolutePath(relative);
        Directory.CreateDirectory(Path.GetDirectoryName(absolute)!);

        await using var file = File.Create(absolute);
        await content.CopyToAsync(file, cancellationToken);
        return relative.Replace('\\', '/');
    }

    public string GetAbsolutePath(string relativePath)
    {
        var normalized = relativePath.Replace('/', Path.DirectorySeparatorChar);
        var absolute = Path.GetFullPath(Path.Combine(RootPath, normalized));
        if (!absolute.StartsWith(RootPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Invalid storage path.");
        }

        return absolute;
    }

    public string GetOutputPath(string projectId, string jobId)
    {
        var relative = Path.Combine("projects", projectId, "renders", $"{jobId}.mp4");
        var absolute = GetAbsolutePath(relative);
        Directory.CreateDirectory(Path.GetDirectoryName(absolute)!);
        return absolute;
    }

    public string GetDownloadUrlPath(string projectId, string jobId) =>
        $"/api/v1/render/{jobId}/download";

    public string GetJobWorkspacePath(string jobId)
    {
        var relative = Path.Combine("temp", "jobs", jobId);
        var absolute = GetAbsolutePath(relative);
        Directory.CreateDirectory(absolute);
        return absolute;
    }

    private static string SanitizeFileName(string fileName)
    {
        var name = Path.GetFileName(fileName);
        foreach (var c in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(c, '_');
        }

        return string.IsNullOrWhiteSpace(name) ? "file.bin" : name;
    }
}
