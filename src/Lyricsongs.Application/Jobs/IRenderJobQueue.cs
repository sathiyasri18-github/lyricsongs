namespace Lyricsongs.Application.Jobs;

public interface IRenderJobQueue
{
    ValueTask EnqueueAsync(string jobId, CancellationToken cancellationToken = default);
    IAsyncEnumerable<string> DequeueAllAsync(CancellationToken cancellationToken);
}
