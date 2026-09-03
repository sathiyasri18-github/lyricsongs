using Lyricsongs.Domain.Lyrics;
using Lyricsongs.Domain.Subtitles;

namespace Lyricsongs.Infrastructure.Lyrics.Renderers;

/// <summary>
/// Bold: larger text, strong outline/shadow, short fade.
/// ASS: \fad + thick border already in style.
/// </summary>
public sealed class BoldLyricStyleRenderer : AssLyricStyleRendererBase
{
    public override LyricAnimationStyle Style => LyricAnimationStyle.Bold;

    protected override string BuildOverrideTags(LyricCue cue, LyricStyleConfiguration config) =>
        $"\\fad({config.FadeInMs},{config.FadeOutMs})\\bord{Math.Max(config.OutlineWidth, 4)}";
}
