using Lyricsongs.Domain.Lyrics;
using Lyricsongs.Domain.Subtitles;

namespace Lyricsongs.Application.Lyrics;

/// <summary>
/// Renders parsed SRT cues into ASS subtitle content and an FFmpeg filter plan for one animation style.
/// </summary>
public interface ILyricStyleRenderer
{
    LyricAnimationStyle Style { get; }

    LyricRenderPlan Render(IReadOnlyList<LyricCue> cues, LyricStyleConfiguration configuration);
}
