using Lyricsongs.Domain.VideoPlan;

namespace Lyricsongs.Application.VideoPlan;

public interface IVideoPlanEngine
{
    /// <summary>
    /// Builds and validates a deterministic VideoPlan from audio duration, lyrics, assets, and settings.
    /// Throws <see cref="VideoPlanException"/> when validation fails with errors.
    /// </summary>
    VideoPlanBuildResult Build(VideoPlanRequest request);

    /// <summary>
    /// Applies user overrides, regenerates the plan, and re-validates.
    /// </summary>
    VideoPlanBuildResult ApplyOverrides(VideoPlanRequest request, VideoPlanSettings overrides);
}

public sealed class VideoPlanBuildResult
{
    public required Domain.VideoPlan.VideoPlan Plan { get; init; }
    public required VideoPlanValidationResult Validation { get; init; }
}

public interface IBackgroundPlanner
{
    IReadOnlyList<BackgroundSegment> Plan(
        long audioDurationMs,
        IReadOnlyList<VideoPlanAsset> assets,
        VideoPlanSettings settings);
}

public interface ILyricStyleManager
{
    LyricStyleSettings Resolve(VideoPlanSettings settings);

    IReadOnlyList<string> AllowedFonts { get; }

    IReadOnlyDictionary<Domain.Lyrics.LyricAnimationStyle, LyricStyleSettings> Presets { get; }
}

public interface IVideoPlanValidator
{
    VideoPlanValidationResult Validate(
        Domain.VideoPlan.VideoPlan plan,
        IReadOnlyList<VideoPlanAsset> assets,
        VideoPlanRequest request);
}
