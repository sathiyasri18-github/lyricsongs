namespace Lyricsongs.Application.Rendering;

public interface IMediaProbeService
{
    Task<long?> ProbeDurationMsAsync(string filePath, CancellationToken cancellationToken = default);
}
