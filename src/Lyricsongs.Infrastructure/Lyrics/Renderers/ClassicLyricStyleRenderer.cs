using Lyricsongs.Domain.Lyrics;
using Lyricsongs.Domain.Subtitles;

namespace Lyricsongs.Infrastructure.Lyrics.Renderers;

/// <summary>
/// Classic: lyric appears, remains visible, fades out.
/// ASS: \fad(fadeIn, fadeOut)
/// </summary>
public sealed class ClassicLyricStyleRenderer : AssLyricStyleRendererBase
{
    public override LyricAnimationStyle Style => LyricAnimationStyle.Classic;

    protected override string BuildOverrideTags(LyricCue cue, LyricStyleConfiguration config) =>
        $"\\fad({config.FadeInMs},{config.FadeOutMs})";
}
