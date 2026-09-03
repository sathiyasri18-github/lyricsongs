using Lyricsongs.Application.VideoPlan;
using Lyricsongs.Domain.Lyrics;
using Lyricsongs.Domain.VideoPlan;

namespace Lyricsongs.Infrastructure.VideoPlan;

/// <summary>
/// Manages predefined lyric style presets. Users select a style and optional safe overrides —
/// never arbitrary FFmpeg filters.
/// </summary>
public sealed class LyricStyleManager : ILyricStyleManager
{
    private static readonly HashSet<string> FontAllowList = new(StringComparer.OrdinalIgnoreCase)
    {
        "Noto Sans Tamil",
        "Noto Serif Tamil",
        "Noto Sans",
        "Hind Madurai",
        "Mukta Malar",
        "Tiro Tamil",
        "Arima Madurai",
        "Meera Inimai",
        "Kavivanar",
        "Baloo Thambi 2",
        "Catamaran",
        "Anek Tamil",
        "Karla Tamil Upright",
        "Rubik",
        "Shantell Sans",
        "Tillana",
        "Poppins",
        "Montserrat",
        "DM Sans",
        "Lato",
        "Roboto",
        "Open Sans",
        "Nunito",
        "Source Sans 3",
        "Inter",
        "Arial",
        "Arial Unicode MS",
        "Segoe UI"
    };

    private readonly Dictionary<LyricAnimationStyle, LyricStyleSettings> _presets =
        LyricStyleCatalog.Presets.ToDictionary(static kv => kv.Key, static kv => Clone(kv.Value));

    public IReadOnlyList<string> AllowedFonts => FontAllowList.OrderBy(f => f).ToList();

    public IReadOnlyDictionary<LyricAnimationStyle, LyricStyleSettings> Presets => _presets;

    public LyricStyleSettings Resolve(VideoPlanSettings settings)
    {
        if (!_presets.TryGetValue(settings.LyricStyle, out var preset))
        {
            throw new VideoPlanException($"Lyric style '{settings.LyricStyle}' is not supported.");
        }

        var resolved = Clone(preset);

        if (!string.IsNullOrWhiteSpace(settings.FontOverride))
        {
            if (!FontAllowList.Contains(settings.FontOverride))
            {
                throw new VideoPlanException(
                    $"Font '{settings.FontOverride}' is not allowed. Allowed fonts: {string.Join(", ", AllowedFonts)}.");
            }

            resolved.Font = settings.FontOverride;
        }

        if (settings.FontSizeOverride is > 0)
        {
            resolved.FontSize = settings.FontSizeOverride.Value;
        }

        if (settings.PositionOverride is not null)
        {
            resolved.Position = settings.PositionOverride.Value;
        }

        if (settings.AlignmentOverride is not null)
        {
            resolved.Alignment = settings.AlignmentOverride.Value;
        }

        if (!string.IsNullOrWhiteSpace(settings.TextColorOverride))
        {
            resolved.TextColor = settings.TextColorOverride;
        }

        if (!string.IsNullOrWhiteSpace(settings.TextColor2Override))
        {
            resolved.TextColor2 = settings.TextColor2Override;
        }

        if (!string.IsNullOrWhiteSpace(settings.TextColor3Override))
        {
            resolved.TextColor3 = settings.TextColor3Override;
        }

        if (!string.IsNullOrWhiteSpace(settings.HighlightColorOverride))
        {
            resolved.HighlightColor = settings.HighlightColorOverride;
        }

        if (settings.OutlineOverride is not null)
        {
            resolved.Outline = settings.OutlineOverride.Value;
        }

        if (settings.ShadowOverride is not null)
        {
            resolved.Shadow = settings.ShadowOverride.Value;
        }

        if (settings.FadeInMsOverride is >= 0)
        {
            resolved.FadeInMs = settings.FadeInMsOverride.Value;
        }

        if (settings.FadeOutMsOverride is >= 0)
        {
            resolved.FadeOutMs = settings.FadeOutMsOverride.Value;
        }

        return resolved;
    }

    public static bool IsAllowedFont(string font) => FontAllowList.Contains(font);

    private static LyricStyleSettings Clone(LyricStyleSettings s) => new()
    {
        Style = s.Style,
        Font = s.Font,
        FontSize = s.FontSize,
        Position = s.Position,
        Alignment = s.Alignment,
        TextColor = s.TextColor,
        TextColor2 = s.TextColor2,
        TextColor3 = s.TextColor3,
        HighlightColor = s.HighlightColor,
        OutlineColor = s.OutlineColor,
        Outline = s.Outline,
        OutlineWidth = s.OutlineWidth,
        Shadow = s.Shadow,
        ShadowColor = s.ShadowColor,
        ShadowDepth = s.ShadowDepth,
        Opacity = s.Opacity,
        FadeInMs = s.FadeInMs,
        FadeOutMs = s.FadeOutMs
    };
}
