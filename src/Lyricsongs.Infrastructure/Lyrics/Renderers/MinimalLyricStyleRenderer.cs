using Lyricsongs.Domain.Lyrics;
using Lyricsongs.Domain.Subtitles;

namespace Lyricsongs.Infrastructure.Lyrics.Renderers;

/// <summary>
/// Minimal: simple fade-in and fade-out only.
/// ASS: \fad(fadeIn, fadeOut)
/// </summary>
public sealed class MinimalLyricStyleRenderer : AssLyricStyleRendererBase
{
    public override LyricAnimationStyle Style => LyricAnimationStyle.Minimal;

    protected override string BuildOverrideTags(LyricCue cue, LyricStyleConfiguration config)
    {
        var fadeIn = Math.Min(config.FadeInMs, cue.DurationMs / 3);
        var fadeOut = Math.Min(config.FadeOutMs, cue.DurationMs / 3);
        return $"\\fad({fadeIn},{fadeOut})";
    }
}
