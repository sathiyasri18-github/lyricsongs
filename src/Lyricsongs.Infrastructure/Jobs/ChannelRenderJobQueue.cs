using System.Threading.Channels;
using Lyricsongs.Application.Jobs;

namespace Lyricsongs.Infrastructure.Jobs;

public sealed class ChannelRenderJobQueue : IRenderJobQueue
{
    private readonly Channel<string> _channel = Channel.CreateUnbounded<string>(
        new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

    public ValueTask EnqueueAsync(string jobId, CancellationToken cancellationToken = default) =>
        _channel.Writer.WriteAsync(jobId, cancellationToken);

    public async IAsyncEnumerable<string> DequeueAllAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var jobId in _channel.Reader.ReadAllAsync(cancellationToken))
        {
            yield return jobId;
        }
    }
}
