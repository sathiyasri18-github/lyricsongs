namespace Lyricsongs.Application.Jobs;

public interface IProjectFileStorage
{
    string RootPath { get; }

    Task<string> SaveProjectFileAsync(
        string projectId,
        string folder,
        string fileName,
        Stream content,
        CancellationToken cancellationToken = default);

    string GetAbsolutePath(string relativePath);
    string GetOutputPath(string projectId, string jobId);
    string GetDownloadUrlPath(string projectId, string jobId);
    string GetJobWorkspacePath(string jobId);
}
