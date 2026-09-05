namespace Lyricsongs.Application.Jobs;

public interface IRenderJobCancellationRegistry
{
    CancellationToken Register(string jobId);
    void Cancel(string jobId);
    void Complete(string jobId);
}
