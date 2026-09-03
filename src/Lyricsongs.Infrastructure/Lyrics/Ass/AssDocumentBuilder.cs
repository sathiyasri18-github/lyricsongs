using System.Globalization;
using System.Text;
using Lyricsongs.Domain.Lyrics;
using Lyricsongs.Domain.Subtitles;

namespace Lyricsongs.Infrastructure.Lyrics.Ass;

internal sealed class AssDocumentBuilder
{
    private readonly LyricStyleConfiguration _config;
    private readonly List<string> _dialogueLines = [];

    public AssDocumentBuilder(LyricStyleConfiguration config)
    {
        _config = config;
    }

    public AssDocumentBuilder AddDialogue(LyricCue cue, string overrideTags, string text, bool preformatted = false)
    {
        var start = FormatAssTime(cue.StartMs);
        var end = FormatAssTime(cue.EndMs);
        var payload = preformatted ? text : FormatPlainDialogueText(text);
        _dialogueLines.Add($"Dialogue: 0,{start},{end},LyricStyle,,0,0,0,,{{{overrideTags}}}{payload}");
        return this;
    }

    public string Build()
    {
        var sb = new StringBuilder();
        sb.AppendLine("[Script Info]");
        sb.AppendLine("Title: Lyricsongs Generated Lyrics");
        sb.AppendLine("ScriptType: v4.00+");
        sb.AppendLine("WrapStyle: 0");
        sb.AppendLine("ScaledBorderAndShadow: yes");
        sb.AppendLine($"PlayResX: {_config.VideoWidth}");
        sb.AppendLine($"PlayResY: {_config.VideoHeight}");
        sb.AppendLine("Collisions: Normal");
        sb.AppendLine();
        sb.AppendLine("[V4+ Styles]");
        sb.AppendLine("Format: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding");
        sb.AppendLine(BuildStyleLine());
        sb.AppendLine();
        sb.AppendLine("[Events]");
        sb.AppendLine("Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text");

        foreach (var line in _dialogueLines)
        {
            sb.AppendLine(line);
        }

        return sb.ToString();
    }

    private string BuildStyleLine()
    {
        var primary = AssColorHelper.ToAssColor(_config.TextColor, _config.Opacity);
        var secondary = AssColorHelper.ToAssColor(_config.HighlightColor, _config.Opacity);
        var outline = AssColorHelper.ToAssColor(_config.OutlineColor, _config.Opacity);
        var shadow = AssColorHelper.ToAssColor(_config.ShadowColor, _config.Opacity * 0.7);
        var outlineWidth = _config.Outline ? _config.OutlineWidth : 0;
        var shadowDepth = _config.Shadow ? _config.ShadowDepth : 0;
        var alignment = AssLayoutHelper.GetAssAlignment(_config);

        return string.Create(CultureInfo.InvariantCulture,
            $"Style: LyricStyle,{_config.Font},{_config.FontSize},{primary},{secondary},{outline},{shadow},0,0,0,0,100,100,0,0,1,{outlineWidth},{shadowDepth},{alignment},{_config.MarginHorizontal},{_config.MarginHorizontal},{_config.MarginVertical},1");
    }

    internal static string FormatAssTime(long ms)
    {
        var hours = ms / 3_600_000;
        var minutes = ms % 3_600_000 / 60_000;
        var seconds = ms % 60_000 / 1_000;
        var centiseconds = ms % 1_000 / 10;
        return $"{hours}:{minutes:D2}:{seconds:D2}.{centiseconds:D2}";
    }

    internal static string EscapeAssText(string text) =>
        text.Replace("\\", "\\\\")
            .Replace("{", "\\{")
            .Replace("}", "\\}")
            .Replace("\r", "");

    internal static string FormatPlainDialogueText(string text)
    {
        var lines = text.Split('\n');
        return string.Join("\\N", lines.Select(EscapeAssText));
    }
}
