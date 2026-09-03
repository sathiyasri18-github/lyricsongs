using Lyricsongs.Domain.Lyrics;
using Lyricsongs.Domain.Subtitles;
using Lyricsongs.Infrastructure.Lyrics.Ass;

namespace Lyricsongs.Infrastructure.Lyrics.Renderers;

/// <summary>
/// Karaoke: entire lyric visible; words/characters progressively highlight.
/// ASS: {\kfXX} tags with SecondaryColour as highlight.
/// </summary>
public sealed class KaraokeLyricStyleRenderer : AssLyricStyleRendererBase
{
    public override LyricAnimationStyle Style => LyricAnimationStyle.Karaoke;

    protected override bool IsPreformattedDialogue => true;

    protected override string BuildOverrideTags(LyricCue cue, LyricStyleConfiguration config) =>
        "\\q2";

    protected override string BuildDialogueText(LyricCue cue, LyricStyleConfiguration config) =>
        KaraokeTextBuilder.Build(cue.Text, cue.DurationMs, config.KaraokeSplit);
}
