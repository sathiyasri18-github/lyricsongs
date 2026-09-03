using Lyricsongs.Domain.Lyrics;
using Lyricsongs.Domain.Subtitles;

namespace Lyricsongs.Application.Lyrics;

/// <summary>
/// Orchestrates lyric animation rendering: resolves style renderer and produces render plan.
/// </summary>
public interface ILyricAnimationEngine
{
    LyricRenderPlan Render(IReadOnlyList<LyricCue> cues, LyricStyleConfiguration configuration);
}
