namespace Lyricsongs.Application.Jobs;

public interface IRenderJobRecovery
{
    Task RecoverAsync(CancellationToken cancellationToken = default);
}
