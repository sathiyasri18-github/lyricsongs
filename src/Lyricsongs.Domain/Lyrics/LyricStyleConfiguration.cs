namespace Lyricsongs.Domain.Lyrics;

/// <summary>
/// User-facing lyric animation configuration (JSON-serializable).
/// </summary>
public sealed class LyricStyleConfiguration
{
    public LyricAnimationStyle Style { get; set; } = LyricAnimationStyle.Classic;

    public string Font { get; set; } = LyricStyleDefaults.Font;

    public int FontSize { get; set; } = LyricStyleDefaults.FontSize;

    public LyricPosition Position { get; set; } = LyricPosition.Bottom;

    public LyricTextAlignment Alignment { get; set; } = LyricTextAlignment.Center;

    public string TextColor { get; set; } = LyricStyleDefaults.TextColor;

    /// <summary>Middle stop for multi-color gradient text (#RRGGBB).</summary>
    public string? TextColor2 { get; set; }

    /// <summary>End stop for multi-color gradient text (#RRGGBB).</summary>
    public string? TextColor3 { get; set; }

    public string HighlightColor { get; set; } = LyricStyleDefaults.HighlightColor;

    public string OutlineColor { get; set; } = LyricStyleDefaults.OutlineColor;

    public bool Outline { get; set; } = true;

    public int OutlineWidth { get; set; } = LyricStyleDefaults.OutlineWidth;

    public bool Shadow { get; set; } = true;

    public string ShadowColor { get; set; } = LyricStyleDefaults.ShadowColor;

    public int ShadowDepth { get; set; } = LyricStyleDefaults.ShadowDepth;

    /// <summary>Opacity from 0.0 (transparent) to 1.0 (opaque).</summary>
    public double Opacity { get; set; } = 1.0;

    public int FadeInMs { get; set; } = LyricStyleDefaults.FadeInMs;

    public int FadeOutMs { get; set; } = LyricStyleDefaults.FadeOutMs;

    public int MarginHorizontal { get; set; } = LyricStyleDefaults.MarginHorizontal;

    public int MarginVertical { get; set; } = LyricStyleDefaults.MarginVertical;

    public int VideoWidth { get; set; } = 1920;

    public int VideoHeight { get; set; } = 1080;

    /// <summary>Karaoke: split unit — Word or Character (grapheme).</summary>
    public KaraokeSplitMode KaraokeSplit { get; set; } = KaraokeSplitMode.Word;

    /// <summary>Pop: starting scale percentage.</summary>
    public int PopStartScalePercent { get; set; } = 90;

    /// <summary>Pop: ending scale percentage.</summary>
    public int PopEndScalePercent { get; set; } = 100;

    /// <summary>Pop: scale animation duration (ms).</summary>
    public int PopAnimationMs { get; set; } = 250;

    /// <summary>Cinematic: upward travel in pixels.</summary>
    public int CinematicRisePixels { get; set; } = 30;
}

public enum KaraokeSplitMode
{
    Word,
    Character
}

public static class LyricStyleDefaults
{
    public const string Font = "Noto Sans Tamil";
    public const int FontSize = 54;
    public const string TextColor = "#FFFFFF";
    public const string HighlightColor = "#FFD700";
    public const string OutlineColor = "#000000";
    public const string ShadowColor = "#000000";
    public const int OutlineWidth = 2;
    public const int ShadowDepth = 2;
    public const int FadeInMs = 200;
    public const int FadeOutMs = 200;
    public const int MarginHorizontal = 40;
    public const int MarginVertical = 60;
}
