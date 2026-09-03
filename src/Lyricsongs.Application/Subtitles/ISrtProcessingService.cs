using Lyricsongs.Domain.Subtitles;

namespace Lyricsongs.Application.Subtitles;

/// <summary>
/// Orchestrates SRT parsing and validation into a complete result.
/// </summary>
public interface ISrtProcessingService
{
    SrtParseResult Process(string srtContent);
    SrtParseResult Process(Stream stream);
}
