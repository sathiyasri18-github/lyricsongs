using Lyricsongs.Domain.Lyrics;
using Lyricsongs.Domain.Subtitles;
using Lyricsongs.Infrastructure.Lyrics.Ass;

namespace Lyricsongs.Infrastructure.Lyrics.Renderers;

/// <summary>
/// Cinematic: fade-in, slight upward movement, fade-out.
/// ASS: \move(x1,y1,x2,y2) + \fad
/// </summary>
public sealed class CinematicLyricStyleRenderer : AssLyricStyleRendererBase
{
    public override LyricAnimationStyle Style => LyricAnimationStyle.Cinematic;

    protected override string BuildOverrideTags(LyricCue cue, LyricStyleConfiguration config)
    {
        var (x, y) = AssLayoutHelper.GetAnchorPoint(config);
        var rise = config.CinematicRisePixels;
        var yStart = y + rise;

        return $"\\fad({config.FadeInMs},{config.FadeOutMs})\\move({x},{yStart},{x},{y})";
    }
}
