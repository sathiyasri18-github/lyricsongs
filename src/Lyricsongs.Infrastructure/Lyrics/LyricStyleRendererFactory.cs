using Lyricsongs.Application.Lyrics;
using Lyricsongs.Domain.Lyrics;

namespace Lyricsongs.Infrastructure.Lyrics;

/// <summary>
/// Resolves lyric style renderers by style enum (strategy registry).
/// </summary>
public sealed class LyricStyleRendererFactory : ILyricStyleRendererFactory
{
    private readonly IReadOnlyDictionary<LyricAnimationStyle, ILyricStyleRenderer> _renderers;

    public LyricStyleRendererFactory(IEnumerable<ILyricStyleRenderer> renderers)
    {
        _renderers = renderers.ToDictionary(r => r.Style);
        SupportedStyles = _renderers.Keys.OrderBy(s => s).ToList();
    }

    public IReadOnlyList<LyricAnimationStyle> SupportedStyles { get; }

    public ILyricStyleRenderer GetRenderer(LyricAnimationStyle style)
    {
        if (!_renderers.TryGetValue(style, out var renderer))
        {
            throw new NotSupportedException($"Lyric animation style '{style}' is not registered.");
        }

        return renderer;
    }
}
