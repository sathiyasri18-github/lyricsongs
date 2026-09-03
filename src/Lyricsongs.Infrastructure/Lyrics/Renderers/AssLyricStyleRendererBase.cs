using Lyricsongs.Application.Lyrics;
using Lyricsongs.Domain.Lyrics;
using Lyricsongs.Domain.Subtitles;
using Lyricsongs.Infrastructure.Lyrics.Ass;

namespace Lyricsongs.Infrastructure.Lyrics.Renderers;

/// <summary>
/// Shared ASS rendering utilities for all lyric animation styles.
/// </summary>
public abstract class AssLyricStyleRendererBase : ILyricStyleRenderer
{
    public abstract LyricAnimationStyle Style { get; }

    public LyricRenderPlan Render(IReadOnlyList<LyricCue> cues, LyricStyleConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(cues);
        ArgumentNullException.ThrowIfNull(configuration);

        var styledCues = new List<LyricStyledCue>(cues.Count);
        var builder = new AssDocumentBuilder(configuration);

        foreach (var cue in cues)
        {
            var tags = BuildOverrideTags(cue, configuration);
            var dialogueText = BuildDialogueText(cue, configuration);
            styledCues.Add(new LyricStyledCue
            {
                Index = cue.Index,
                StartMs = cue.StartMs,
                EndMs = cue.EndMs,
                Text = cue.Text,
                AssOverrideTags = tags,
                AssDialogueText = dialogueText
            });

            builder.AddDialogue(cue, tags, dialogueText, IsPreformattedDialogue);
        }

        var assContent = builder.Build();

        return new LyricRenderPlan
        {
            Style = Style,
            Configuration = configuration,
            AssContent = assContent,
            Ffmpeg = FfmpegLyricFilterBuilder.Build(configuration, Style),
            StyledCues = styledCues
        };
    }

    protected abstract string BuildOverrideTags(LyricCue cue, LyricStyleConfiguration config);

    protected virtual bool IsPreformattedDialogue => false;

    protected virtual string BuildDialogueText(LyricCue cue, LyricStyleConfiguration config) =>
        cue.Text;
}
