using System.Text.Json.Serialization;
using Lyricsongs.Domain.Lyrics;

namespace Lyricsongs.Application.Lyrics.Models;

/// <summary>
/// API/JSON-facing lyric style configuration (camelCase).
/// </summary>
public sealed class LyricStyleConfigurationDto
{
    public string Style { get; set; } = "classic";
    public string Font { get; set; } = LyricStyleDefaults.Font;
    public int FontSize { get; set; } = LyricStyleDefaults.FontSize;
    public string Position { get; set; } = "bottom";
    public string Alignment { get; set; } = "center";
    public string TextColor { get; set; } = LyricStyleDefaults.TextColor;
    public string HighlightColor { get; set; } = LyricStyleDefaults.HighlightColor;
    public string OutlineColor { get; set; } = LyricStyleDefaults.OutlineColor;
    public bool Outline { get; set; } = true;
    public int OutlineWidth { get; set; } = LyricStyleDefaults.OutlineWidth;
    public bool Shadow { get; set; } = true;
    public string ShadowColor { get; set; } = LyricStyleDefaults.ShadowColor;
    public int ShadowDepth { get; set; } = LyricStyleDefaults.ShadowDepth;

    [JsonPropertyName("opacity")]
    public double Opacity { get; set; } = 1.0;

    public int FadeInMs { get; set; } = LyricStyleDefaults.FadeInMs;
    public int FadeOutMs { get; set; } = LyricStyleDefaults.FadeOutMs;
    public int MarginHorizontal { get; set; } = LyricStyleDefaults.MarginHorizontal;
    public int MarginVertical { get; set; } = LyricStyleDefaults.MarginVertical;
    public int VideoWidth { get; set; } = 1920;
    public int VideoHeight { get; set; } = 1080;
    public string KaraokeSplit { get; set; } = "word";
    public int PopStartScalePercent { get; set; } = 90;
    public int PopEndScalePercent { get; set; } = 100;
    public int PopAnimationMs { get; set; } = 250;
    public int CinematicRisePixels { get; set; } = 30;

    public LyricStyleConfiguration ToConfiguration() => new()
    {
        Style = Enum.Parse<LyricAnimationStyle>(Style, ignoreCase: true),
        Font = Font,
        FontSize = FontSize,
        Position = Enum.Parse<LyricPosition>(Position, ignoreCase: true),
        Alignment = Enum.Parse<LyricTextAlignment>(Alignment, ignoreCase: true),
        TextColor = TextColor,
        HighlightColor = HighlightColor,
        OutlineColor = OutlineColor,
        Outline = Outline,
        OutlineWidth = OutlineWidth,
        Shadow = Shadow,
        ShadowColor = ShadowColor,
        ShadowDepth = ShadowDepth,
        Opacity = Opacity,
        FadeInMs = FadeInMs,
        FadeOutMs = FadeOutMs,
        MarginHorizontal = MarginHorizontal,
        MarginVertical = MarginVertical,
        VideoWidth = VideoWidth,
        VideoHeight = VideoHeight,
        KaraokeSplit = Enum.Parse<KaraokeSplitMode>(KaraokeSplit, ignoreCase: true),
        PopStartScalePercent = PopStartScalePercent,
        PopEndScalePercent = PopEndScalePercent,
        PopAnimationMs = PopAnimationMs,
        CinematicRisePixels = CinematicRisePixels
    };
}

public sealed class LyricRenderPlanDto
{
    public string Style { get; init; } = string.Empty;
    public LyricStyleConfigurationDto Configuration { get; init; } = new();
    public string AssContent { get; init; } = string.Empty;
    public LyricFfmpegFilterPlanDto Ffmpeg { get; init; } = new();
    public IReadOnlyList<LyricStyledCueDto> StyledCues { get; init; } = [];
}

public sealed class LyricFfmpegFilterPlanDto
{
    public string SubtitleFilter { get; init; } = string.Empty;
    public IReadOnlyList<string> FilterSteps { get; init; } = [];
    public string Description { get; init; } = string.Empty;
    public string AssFileName { get; init; } = "lyrics.ass";
}

public sealed class LyricStyledCueDto
{
    public int Index { get; init; }
    public long StartMs { get; init; }
    public long EndMs { get; init; }
    public string Text { get; init; } = string.Empty;
    public string AssOverrideTags { get; init; } = string.Empty;
    public string AssDialogueText { get; init; } = string.Empty;
}

public static class LyricRenderPlanMapper
{
    public static LyricRenderPlanDto ToDto(LyricRenderPlan plan) => new()
    {
        Style = plan.Style.ToString(),
        Configuration = ToConfigDto(plan.Configuration),
        AssContent = plan.AssContent,
        Ffmpeg = new LyricFfmpegFilterPlanDto
        {
            SubtitleFilter = plan.Ffmpeg.SubtitleFilter,
            FilterSteps = plan.Ffmpeg.FilterSteps.ToList(),
            Description = plan.Ffmpeg.Description,
            AssFileName = plan.Ffmpeg.AssFileName
        },
        StyledCues = plan.StyledCues.Select(c => new LyricStyledCueDto
        {
            Index = c.Index,
            StartMs = c.StartMs,
            EndMs = c.EndMs,
            Text = c.Text,
            AssOverrideTags = c.AssOverrideTags,
            AssDialogueText = c.AssDialogueText
        }).ToList()
    };

    private static LyricStyleConfigurationDto ToConfigDto(LyricStyleConfiguration c) => new()
    {
        Style = c.Style.ToString().ToLowerInvariant(),
        Font = c.Font,
        FontSize = c.FontSize,
        Position = c.Position.ToString().ToLowerInvariant(),
        Alignment = c.Alignment.ToString().ToLowerInvariant(),
        TextColor = c.TextColor,
        HighlightColor = c.HighlightColor,
        OutlineColor = c.OutlineColor,
        Outline = c.Outline,
        OutlineWidth = c.OutlineWidth,
        Shadow = c.Shadow,
        ShadowColor = c.ShadowColor,
        ShadowDepth = c.ShadowDepth,
        Opacity = c.Opacity,
        FadeInMs = c.FadeInMs,
        FadeOutMs = c.FadeOutMs,
        MarginHorizontal = c.MarginHorizontal,
        MarginVertical = c.MarginVertical,
        VideoWidth = c.VideoWidth,
        VideoHeight = c.VideoHeight,
        KaraokeSplit = c.KaraokeSplit.ToString().ToLowerInvariant(),
        PopStartScalePercent = c.PopStartScalePercent,
        PopEndScalePercent = c.PopEndScalePercent,
        PopAnimationMs = c.PopAnimationMs,
        CinematicRisePixels = c.CinematicRisePixels
    };
}
