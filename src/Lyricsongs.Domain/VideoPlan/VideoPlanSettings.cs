using Lyricsongs.Domain.Lyrics;
using Lyricsongs.Domain.Subtitles;

namespace Lyricsongs.Domain.VideoPlan;

/// <summary>
/// User-selected settings and optional overrides for VideoPlan generation.
/// </summary>
public sealed class VideoPlanSettings
{
    public BackgroundPlanningMode PlanningMode { get; set; } = BackgroundPlanningMode.Sequential;

    /// <summary>Seed for Random mode. Same seed + inputs → same plan.</summary>
    public int RandomSeed { get; set; } = 42;

    public TransitionSettings Transition { get; set; } = new();

    public long MinSegmentDurationMs { get; set; } = 3_000;

    public LyricAnimationStyle LyricStyle { get; set; } = LyricAnimationStyle.Cinematic;

    public VideoResolutionPreset ResolutionPreset { get; set; } = VideoResolutionPreset.FullHd1080p;

    public int Fps { get; set; } = 30;

    public BackgroundAnimation DefaultImageAnimation { get; set; } = BackgroundAnimation.ZoomIn;

    /// <summary>When true, cycle through animations across image segments.</summary>
    public bool CycleImageAnimations { get; set; } = true;

    // Optional overrides (null = use style defaults)
    public string? FontOverride { get; set; }
    public int? FontSizeOverride { get; set; }
    public LyricPosition? PositionOverride { get; set; }
    public LyricTextAlignment? AlignmentOverride { get; set; }
    public string? TextColorOverride { get; set; }
    public string? TextColor2Override { get; set; }
    public string? TextColor3Override { get; set; }
    public string? HighlightColorOverride { get; set; }
    public bool? OutlineOverride { get; set; }
    public bool? ShadowOverride { get; set; }
    public int? FadeInMsOverride { get; set; }
    public int? FadeOutMsOverride { get; set; }

    /// <summary>Explicit asset order (asset IDs). Null = derive from planning mode.</summary>
    public IReadOnlyList<string>? AssetOrderOverride { get; set; }

    /// <summary>
    /// Manual background segment overrides. When provided, planner uses these durations
    /// instead of automatic distribution (still validated).
    /// </summary>
    public IReadOnlyList<ManualBackgroundOverride>? ManualBackgrounds { get; set; }
}

public sealed class ManualBackgroundOverride
{
    public required string AssetId { get; init; }
    public required long StartMs { get; init; }
    public required long EndMs { get; init; }
    public BackgroundAnimation? Animation { get; init; }
    public TransitionType? TransitionOut { get; init; }
    public long? TransitionOutDurationMs { get; init; }
}

/// <summary>
/// Full input to the Video Plan Engine.
/// </summary>
public sealed class VideoPlanRequest
{
    public required long AudioDurationMs { get; init; }
    public required IReadOnlyList<LyricCue> Lyrics { get; init; }
    public required IReadOnlyList<VideoPlanAsset> Assets { get; init; }
    public required VideoPlanSettings Settings { get; init; }
}
