using Lyricsongs.Domain.Lyrics;
using Lyricsongs.Domain.Subtitles;

namespace Lyricsongs.Infrastructure.Lyrics.Renderers;

/// <summary>
/// Pop: lyric scales from 90% to 100% with slight fade-in.
/// ASS: \fscx/\fscy + \t transform + \fad
/// </summary>
public sealed class PopLyricStyleRenderer : AssLyricStyleRendererBase
{
    public override LyricAnimationStyle Style => LyricAnimationStyle.Pop;

    protected override string BuildOverrideTags(LyricCue cue, LyricStyleConfiguration config)
    {
        var start = config.PopStartScalePercent;
        var end = config.PopEndScalePercent;
        var animMs = Math.Min(config.PopAnimationMs, cue.DurationMs / 2);

        return $"\\fad({config.FadeInMs},0)\\fscx{start}\\fscy{start}\\t(0,{animMs},\\fscx{end}\\fscy{end})";
    }
}
