using Lyricsongs.Domain.Subtitles;

namespace Lyricsongs.Domain.VideoPlan;

/// <summary>
/// Deterministic, validated plan consumed by the FFmpeg rendering engine.
/// Contains no FFmpeg commands — structure only.
/// </summary>
public sealed class VideoPlan
{
    public required string PlanId { get; init; }
    public required long AudioDurationMs { get; init; }
    public required VideoOutputSettings Output { get; init; }
    public required IReadOnlyList<BackgroundSegment> Backgrounds { get; init; }
    public required LyricStyleSettings Lyrics { get; init; }
    public required TransitionSettings Transition { get; init; }
    public required IReadOnlyList<LyricCue> Subtitles { get; init; }
    public required IReadOnlyList<string> OrderedAssetIds { get; init; }
    public required BackgroundPlanningMode PlanningMode { get; init; }
    public required int RandomSeed { get; init; }
}
