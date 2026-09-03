using Lyricsongs.Application.Lyrics;
using Lyricsongs.Domain.Lyrics;
using Lyricsongs.Domain.Subtitles;

namespace Lyricsongs.Infrastructure.Lyrics;

/// <summary>
/// Orchestrates lyric rendering by delegating to the configured style renderer.
/// </summary>
public sealed class LyricAnimationEngine : ILyricAnimationEngine
{
    private readonly ILyricStyleRendererFactory _rendererFactory;

    public LyricAnimationEngine(ILyricStyleRendererFactory rendererFactory)
    {
        _rendererFactory = rendererFactory;
    }

    public LyricRenderPlan Render(IReadOnlyList<LyricCue> cues, LyricStyleConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(cues);
        ArgumentNullException.ThrowIfNull(configuration);

        var renderer = _rendererFactory.GetRenderer(configuration.Style);
        return renderer.Render(cues, configuration);
    }
}
