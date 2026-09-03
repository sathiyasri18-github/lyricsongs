using Lyricsongs.Domain.Lyrics;

namespace Lyricsongs.Domain.VideoPlan;

/// <summary>
/// Resolved lyric presentation settings. Built from a predefined style plus optional overrides.
/// </summary>
public sealed class LyricStyleSettings
{
    public LyricAnimationStyle Style { get; set; } = LyricAnimationStyle.Cinematic;
    public string Font { get; set; } = "Noto Sans Tamil";
    public int FontSize { get; set; } = 54;
    public LyricPosition Position { get; set; } = LyricPosition.Bottom;
    public LyricTextAlignment Alignment { get; set; } = LyricTextAlignment.Center;
    public string TextColor { get; set; } = "#FFFFFF";
    public string? TextColor2 { get; set; }
    public string? TextColor3 { get; set; }
    public string HighlightColor { get; set; } = "#FFD700";
    public string OutlineColor { get; set; } = "#000000";
    public bool Outline { get; set; } = true;
    public int OutlineWidth { get; set; } = 2;
    public bool Shadow { get; set; } = true;
    public string ShadowColor { get; set; } = "#000000";
    public int ShadowDepth { get; set; } = 2;
    public double Opacity { get; set; } = 1.0;
    public int FadeInMs { get; set; } = 300;
    public int FadeOutMs { get; set; } = 300;
}
