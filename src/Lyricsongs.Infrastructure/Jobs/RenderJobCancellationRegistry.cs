using System.Collections.Concurrent;
using Lyricsongs.Application.Jobs;

namespace Lyricsongs.Infrastructure.Jobs;

public sealed class RenderJobCancellationRegistry : IRenderJobCancellationRegistry
{
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _tokens = new(StringComparer.Ordinal);

    public CancellationToken Register(string jobId)
    {
        var cts = new CancellationTokenSource();
        var existing = _tokens.AddOrUpdate(
            jobId,
            cts,
            (_, previous) =>
            {
                previous.Dispose();
                return cts;
            });

        return existing.Token;
    }

    public void Cancel(string jobId)
    {
        if (_tokens.TryGetValue(jobId, out var cts))
        {
            cts.Cancel();
        }
    }

    public void Complete(string jobId)
    {
        if (_tokens.TryRemove(jobId, out var cts))
        {
            cts.Dispose();
        }
    }
}
