using Lyricsongs.Domain.Lyrics;

namespace Lyricsongs.Application.Lyrics;

/// <summary>
/// Resolves the appropriate style renderer from configuration (strategy factory).
/// </summary>
public interface ILyricStyleRendererFactory
{
    ILyricStyleRenderer GetRenderer(LyricAnimationStyle style);

    IReadOnlyList<LyricAnimationStyle> SupportedStyles { get; }
}
