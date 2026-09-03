using Lyricsongs.Domain.Lyrics;
using Lyricsongs.Domain.VideoPlan;

namespace Lyricsongs.Infrastructure.VideoPlan;

public static class LyricStyleCatalog
{
    public static IReadOnlyDictionary<LyricAnimationStyle, LyricStyleSettings> Presets { get; } =
        BuildPresets();

    public static IReadOnlyList<LyricAnimationStyle> AllStyles { get; } =
        Enum.GetValues<LyricAnimationStyle>().OrderBy(s => s.ToString()).ToList();

    private static Dictionary<LyricAnimationStyle, LyricStyleSettings> BuildPresets()
    {
        var presets = new Dictionary<LyricAnimationStyle, LyricStyleSettings>();

        void Add(
            LyricAnimationStyle style,
            string font,
            int fontSize,
            LyricPosition position,
            string textColor,
            string highlightColor,
            bool outline,
            int outlineWidth,
            bool shadow,
            int shadowDepth,
            int fadeInMs,
            int fadeOutMs)
        {
            presets[style] = new LyricStyleSettings
            {
                Style = style,
                Font = font,
                FontSize = fontSize,
                Position = position,
                Alignment = LyricTextAlignment.Center,
                TextColor = textColor,
                HighlightColor = highlightColor,
                Outline = outline,
                OutlineWidth = outlineWidth,
                Shadow = shadow,
                ShadowDepth = shadowDepth,
                FadeInMs = fadeInMs,
                FadeOutMs = fadeOutMs
            };
        }

        Add(LyricAnimationStyle.Classic, "Noto Sans Tamil", 54, LyricPosition.Bottom, "#FFFFFF", "#FFD700", true, 2, true, 2, 200, 200);
        Add(LyricAnimationStyle.Karaoke, "Noto Sans Tamil", 54, LyricPosition.Bottom, "#FFFFFF", "#FFD700", true, 2, true, 2, 0, 100);
        Add(LyricAnimationStyle.Pop, "Montserrat", 58, LyricPosition.Center, "#FFFFFF", "#FF6B9D", true, 3, true, 2, 200, 0);
        Add(LyricAnimationStyle.Cinematic, "Noto Sans Tamil", 54, LyricPosition.Bottom, "#FFFFFF", "#FFD700", true, 2, true, 3, 300, 300);
        Add(LyricAnimationStyle.Minimal, "Noto Sans", 48, LyricPosition.Bottom, "#FFFFFF", "#FFFFFF", false, 0, false, 0, 150, 150);
        Add(LyricAnimationStyle.Bold, "Noto Sans Tamil", 64, LyricPosition.Bottom, "#FFFFFF", "#FFD700", true, 4, true, 4, 150, 150);
        Add(LyricAnimationStyle.Neon, "Rubik", 56, LyricPosition.Center, "#00FFFF", "#FF00FF", true, 2, true, 4, 250, 200);
        Add(LyricAnimationStyle.Glow, "Poppins", 54, LyricPosition.Bottom, "#FFFFFF", "#FFE066", true, 1, true, 5, 300, 250);
        Add(LyricAnimationStyle.Bounce, "Baloo Thambi 2", 58, LyricPosition.Center, "#FFFFFF", "#FF9800", true, 3, true, 2, 150, 100);
        Add(LyricAnimationStyle.SlideUp, "Catamaran", 54, LyricPosition.Bottom, "#FFFFFF", "#90CAF9", true, 2, true, 2, 200, 200);
        Add(LyricAnimationStyle.SlideDown, "Hind Madurai", 54, LyricPosition.Top, "#FFFFFF", "#FFD700", true, 2, true, 2, 200, 200);
        Add(LyricAnimationStyle.SlideLeft, "Mukta Malar", 54, LyricPosition.Center, "#FFFFFF", "#FFD700", true, 2, true, 2, 200, 200);
        Add(LyricAnimationStyle.SlideRight, "Arima Madurai", 54, LyricPosition.Center, "#FFFFFF", "#FFD700", true, 2, true, 2, 200, 200);
        Add(LyricAnimationStyle.Typewriter, "Roboto", 50, LyricPosition.Bottom, "#FFFFFF", "#FFD700", true, 2, false, 0, 0, 150);
        Add(LyricAnimationStyle.Wave, "Shantell Sans", 54, LyricPosition.Center, "#FFFFFF", "#FF80AB", true, 2, true, 2, 200, 200);
        Add(LyricAnimationStyle.Pulse, "Nunito", 56, LyricPosition.Center, "#FFFFFF", "#FF5252", true, 2, true, 2, 100, 100);
        Add(LyricAnimationStyle.Shake, "Montserrat", 54, LyricPosition.Bottom, "#FFFFFF", "#FFD700", true, 3, true, 3, 100, 100);
        Add(LyricAnimationStyle.Rotate, "Lato", 54, LyricPosition.Center, "#FFFFFF", "#FFD700", true, 2, true, 2, 250, 200);
        Add(LyricAnimationStyle.BlurIn, "Open Sans", 54, LyricPosition.Bottom, "#FFFFFF", "#FFD700", true, 2, true, 2, 400, 300);
        Add(LyricAnimationStyle.Shadow, "Noto Serif Tamil", 56, LyricPosition.Bottom, "#FFFFFF", "#FFD700", false, 0, true, 6, 250, 250);
        Add(LyricAnimationStyle.Outline, "Noto Sans Tamil", 58, LyricPosition.Bottom, "#FFFFFF", "#FFD700", true, 6, false, 0, 200, 200);
        Add(LyricAnimationStyle.Gradient, "Anek Tamil", 54, LyricPosition.Center, "#FFFFFF", "#00E676", true, 2, true, 2, 200, 150);
        Add(LyricAnimationStyle.Retro, "Tillana", 52, LyricPosition.Bottom, "#FFE082", "#FF7043", true, 3, true, 2, 200, 200);
        Add(LyricAnimationStyle.Elegant, "Tiro Tamil", 50, LyricPosition.Bottom, "#F5F5F5", "#E0E0E0", false, 1, true, 2, 500, 500);

        Add(LyricAnimationStyle.ZoomIn, "Montserrat", 56, LyricPosition.Center, "#FFFFFF", "#FFD700", true, 2, true, 2, 200, 200);
        Add(LyricAnimationStyle.ZoomOut, "Poppins", 56, LyricPosition.Center, "#FFFFFF", "#90CAF9", true, 2, true, 2, 200, 200);
        Add(LyricAnimationStyle.DropIn, "Baloo Thambi 2", 58, LyricPosition.Center, "#FFFFFF", "#FF9800", true, 3, true, 3, 150, 150);
        Add(LyricAnimationStyle.FlipIn, "Catamaran", 54, LyricPosition.Bottom, "#FFFFFF", "#FFD700", true, 2, true, 2, 250, 200);
        Add(LyricAnimationStyle.Spin, "Rubik", 54, LyricPosition.Center, "#FFFFFF", "#FF6B9D", true, 2, true, 2, 200, 200);
        Add(LyricAnimationStyle.Drift, "Nunito", 52, LyricPosition.Bottom, "#FFFFFF", "#80DEEA", true, 2, true, 2, 300, 300);
        Add(LyricAnimationStyle.Float, "Shantell Sans", 54, LyricPosition.Center, "#FFFFFF", "#CE93D8", true, 2, true, 2, 300, 300);
        Add(LyricAnimationStyle.Elastic, "Montserrat", 58, LyricPosition.Center, "#FFFFFF", "#FF5252", true, 3, true, 2, 150, 150);
        Add(LyricAnimationStyle.Slam, "Noto Sans Tamil", 62, LyricPosition.Center, "#FFFFFF", "#FFD700", true, 4, true, 4, 100, 150);
        Add(LyricAnimationStyle.Whisper, "Tiro Tamil", 48, LyricPosition.Bottom, "#F5F5F5", "#E0E0E0", false, 0, true, 1, 400, 400);
        Add(LyricAnimationStyle.SoftGlow, "Poppins", 54, LyricPosition.Bottom, "#FFFFFF", "#FFE066", true, 1, true, 5, 350, 300);
        Add(LyricAnimationStyle.HardCut, "Noto Sans Tamil", 54, LyricPosition.Bottom, "#FFFFFF", "#FFD700", true, 2, false, 0, 0, 0);
        Add(LyricAnimationStyle.Glitch, "Roboto", 54, LyricPosition.Center, "#00FFCC", "#FF00AA", true, 2, true, 2, 100, 100);
        Add(LyricAnimationStyle.RiseFade, "Noto Serif Tamil", 52, LyricPosition.Bottom, "#FFFFFF", "#FFD700", true, 2, true, 3, 450, 450);
        Add(LyricAnimationStyle.Cascade, "Anek Tamil", 54, LyricPosition.Center, "#FFFFFF", "#81C784", true, 2, true, 2, 250, 200);
        Add(LyricAnimationStyle.SoftSlide, "Hind Madurai", 52, LyricPosition.Bottom, "#FFFFFF", "#90CAF9", true, 2, true, 2, 280, 280);
        Add(LyricAnimationStyle.Instant, "Noto Sans Tamil", 54, LyricPosition.Bottom, "#FFFFFF", "#FFD700", true, 2, false, 0, 0, 0);
        Add(LyricAnimationStyle.SlowReveal, "Open Sans", 50, LyricPosition.Bottom, "#FFFFFF", "#FFD700", true, 2, true, 2, 0, 200);
        Add(LyricAnimationStyle.Swing, "Lato", 54, LyricPosition.Center, "#FFFFFF", "#FFB74D", true, 2, true, 2, 200, 200);
        Add(LyricAnimationStyle.ZoomPulse, "Nunito", 56, LyricPosition.Center, "#FFFFFF", "#EF5350", true, 2, true, 2, 150, 150);
        Add(LyricAnimationStyle.Echo, "Noto Serif Tamil", 54, LyricPosition.Bottom, "#FFFFFF", "#FFD700", false, 0, true, 8, 250, 250);
        Add(LyricAnimationStyle.Spotlight, "Montserrat", 56, LyricPosition.Center, "#FFFFFF", "#FFF59D", true, 3, true, 3, 350, 250);

        return presets;
    }
}
