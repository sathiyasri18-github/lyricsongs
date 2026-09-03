namespace Lyricsongs.Domain.Lyrics;

/// <summary>
/// Complete lyric render output: ASS subtitle document + FFmpeg filter plan.
/// </summary>
public sealed class LyricRenderPlan
{
    public required LyricAnimationStyle Style { get; init; }
    public required LyricStyleConfiguration Configuration { get; init; }
    public required string AssContent { get; init; }
    public required LyricFfmpegFilterPlan Ffmpeg { get; init; }
    public required IReadOnlyList<LyricStyledCue> StyledCues { get; init; }
}
