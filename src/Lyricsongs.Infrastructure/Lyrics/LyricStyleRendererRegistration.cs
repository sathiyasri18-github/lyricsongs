using Lyricsongs.Application.Lyrics;
using Lyricsongs.Domain.Lyrics;
using Lyricsongs.Infrastructure.Lyrics.Renderers;
using Lyricsongs.Infrastructure.VideoPlan;

namespace Lyricsongs.Infrastructure.Lyrics;

public static class LyricStyleRendererRegistration
{
    public static IReadOnlyList<ILyricStyleRenderer> CreateAllRenderers() =>
        LyricStyleCatalog.AllStyles
            .Select(style => (ILyricStyleRenderer)new CatalogLyricStyleRenderer(style))
            .ToList();
}
