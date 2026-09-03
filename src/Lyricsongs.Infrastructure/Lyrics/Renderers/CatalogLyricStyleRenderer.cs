using Lyricsongs.Domain.Lyrics;
using Lyricsongs.Domain.Subtitles;
using Lyricsongs.Infrastructure.Lyrics.Ass;

namespace Lyricsongs.Infrastructure.Lyrics.Renderers;

/// <summary>
/// Catalog-driven ASS renderer for all predefined lyric animation styles.
/// </summary>
public sealed class CatalogLyricStyleRenderer : AssLyricStyleRendererBase
{
    public CatalogLyricStyleRenderer(LyricAnimationStyle style)
    {
        Style = style;
    }

    public override LyricAnimationStyle Style { get; }

    protected override bool IsPreformattedDialogue => LyricStyleAssTagBuilder.IsPreformatted(Style);

    protected override string BuildOverrideTags(LyricCue cue, LyricStyleConfiguration config) =>
        LyricStyleAssTagBuilder.BuildOverrideTags(Style, cue, config);

    protected override string BuildDialogueText(LyricCue cue, LyricStyleConfiguration config)
    {
        var text = LyricStyleAssTagBuilder.BuildDialogueText(Style, cue, config);
        if (!IsPreformattedDialogue && GradientTextBuilder.HasGradient(config))
        {
            return GradientTextBuilder.ApplyPlainText(text, config);
        }

        return text;
    }
}
