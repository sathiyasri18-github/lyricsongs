using System.Text.Json;
using System.Text.Json.Serialization;
using Lyricsongs.Domain.Lyrics;
using Lyricsongs.Domain.Timeline;
using Lyricsongs.Domain.VideoPlan;

namespace Lyricsongs.Application.VideoPlan.Models;

public sealed class VideoPlanDto
{
    public string PlanId { get; init; } = string.Empty;
    public long AudioDurationMs { get; init; }
    public VideoOutputDto Output { get; init; } = new();
    public IReadOnlyList<BackgroundSegmentDto> Backgrounds { get; init; } = [];
    public LyricStyleDto Lyrics { get; init; } = new();
    public TransitionDto Transition { get; init; } = new();
    public IReadOnlyList<SubtitleCueDto> Subtitles { get; init; } = [];
    public IReadOnlyList<string> OrderedAssetIds { get; init; } = [];
    public string PlanningMode { get; init; } = "sequential";
    public int RandomSeed { get; init; }
}

public sealed class VideoOutputDto
{
    public int Width { get; init; }
    public int Height { get; init; }
    public int Fps { get; init; }
    public string VideoCodec { get; init; } = "libx264";
    public string AudioCodec { get; init; } = "aac";
    public string Format { get; init; } = "mp4";
}

public sealed class BackgroundSegmentDto
{
    public string AssetId { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public double Start { get; init; }
    public double End { get; init; }
    public string Animation { get; init; } = "none";
    public string TransitionOut { get; init; } = "none";
    public long TransitionOutDurationMs { get; init; }
    public string PlaybackMode { get; init; } = "normal";
    public int LoopCount { get; init; } = 1;
}

public sealed class LyricStyleDto
{
    public string Style { get; init; } = "cinematic";
    public string Font { get; init; } = "Noto Sans Tamil";
    public int FontSize { get; init; } = 54;
    public string Position { get; init; } = "bottom";
    public string Alignment { get; init; } = "center";
    public string TextColor { get; init; } = "#FFFFFF";
    public string? TextColor2 { get; init; }
    public string? TextColor3 { get; init; }
    public string HighlightColor { get; init; } = "#FFD700";
    public bool Outline { get; init; } = true;
    public bool Shadow { get; init; } = true;
    public int FadeIn { get; init; } = 300;
    public int FadeOut { get; init; } = 300;
}

public sealed class TransitionDto
{
    public string Type { get; init; } = "fade";
    public long DurationMs { get; init; } = 500;
}

public sealed class SubtitleCueDto
{
    public int Index { get; init; }
    public string Start { get; init; } = string.Empty;
    public string End { get; init; } = string.Empty;
    public string Text { get; init; } = string.Empty;
    public long StartMs { get; init; }
    public long EndMs { get; init; }
}

public static class VideoPlanDtoMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public static VideoPlanDto ToDto(Domain.VideoPlan.VideoPlan plan) => new()
    {
        PlanId = plan.PlanId,
        AudioDurationMs = plan.AudioDurationMs,
        Output = new VideoOutputDto
        {
            Width = plan.Output.Width,
            Height = plan.Output.Height,
            Fps = plan.Output.Fps,
            VideoCodec = plan.Output.VideoCodec,
            AudioCodec = plan.Output.AudioCodec,
            Format = plan.Output.Format
        },
        Backgrounds = plan.Backgrounds.Select(b => new BackgroundSegmentDto
        {
            AssetId = b.AssetId,
            Type = b.Type == BackgroundAssetType.Image ? "image" : "video",
            Start = b.StartSeconds,
            End = b.EndSeconds,
            Animation = ToCamel(b.Animation.ToString()),
            TransitionOut = ToCamel(b.TransitionOut.ToString()),
            TransitionOutDurationMs = b.TransitionOutDurationMs,
            PlaybackMode = ToCamel(b.PlaybackMode.ToString()),
            LoopCount = b.LoopCount
        }).ToList(),
        Lyrics = new LyricStyleDto
        {
            Style = ToCamel(plan.Lyrics.Style.ToString()),
            Font = plan.Lyrics.Font,
            FontSize = plan.Lyrics.FontSize,
            Position = ToCamel(plan.Lyrics.Position.ToString()),
            Alignment = ToCamel(plan.Lyrics.Alignment.ToString()),
            TextColor = plan.Lyrics.TextColor,
            TextColor2 = plan.Lyrics.TextColor2,
            TextColor3 = plan.Lyrics.TextColor3,
            HighlightColor = plan.Lyrics.HighlightColor,
            Outline = plan.Lyrics.Outline,
            Shadow = plan.Lyrics.Shadow,
            FadeIn = plan.Lyrics.FadeInMs,
            FadeOut = plan.Lyrics.FadeOutMs
        },
        Transition = new TransitionDto
        {
            Type = ToCamel(plan.Transition.Type.ToString()),
            DurationMs = plan.Transition.DurationMs
        },
        Subtitles = plan.Subtitles.Select(s => new SubtitleCueDto
        {
            Index = s.Index,
            Start = s.Start,
            End = s.End,
            Text = s.Text,
            StartMs = s.StartMs,
            EndMs = s.EndMs
        }).ToList(),
        OrderedAssetIds = plan.OrderedAssetIds.ToList(),
        PlanningMode = ToCamel(plan.PlanningMode.ToString()),
        RandomSeed = plan.RandomSeed
    };

    public static string ToJson(Domain.VideoPlan.VideoPlan plan) =>
        JsonSerializer.Serialize(ToDto(plan), JsonOptions);

    private static string ToCamel(string value) =>
        string.IsNullOrEmpty(value)
            ? value
            : char.ToLowerInvariant(value[0]) + value[1..];
}
