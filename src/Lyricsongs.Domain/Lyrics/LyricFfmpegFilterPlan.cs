namespace Lyricsongs.Domain.Lyrics;

/// <summary>
/// FFmpeg filter graph plan for burning styled lyrics onto video.
/// </summary>
public sealed class LyricFfmpegFilterPlan
{
    /// <summary>Primary subtitle filter, e.g. ass='lyrics.ass'.</summary>
    public required string SubtitleFilter { get; init; }

    /// <summary>Ordered filter steps for documentation / pipeline assembly.</summary>
    public required IReadOnlyList<string> FilterSteps { get; init; }

    /// <summary>Human-readable explanation of the filter chain.</summary>
    public required string Description { get; init; }

    /// <summary>Suggested ASS file name for the render pipeline.</summary>
    public string AssFileName { get; init; } = "lyrics.ass";
}
