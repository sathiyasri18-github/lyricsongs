using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Lyricsongs.Domain.Lyrics;

namespace Lyricsongs.Infrastructure.Lyrics.Ass;

internal static class GradientTextBuilder
{
    public static bool HasGradient(LyricStyleConfiguration config) =>
        !string.IsNullOrWhiteSpace(config.TextColor2) || !string.IsNullOrWhiteSpace(config.TextColor3);

    public static string ApplyPlainText(string text, LyricStyleConfiguration config)
    {
        if (!HasGradient(config))
        {
            return text;
        }

        var colors = ResolveColors(config);
        var lines = text.Split('\n');
        var output = new List<string>(lines.Length);

        foreach (var line in lines)
        {
            output.Add(ApplyToLine(line, colors));
        }

        return string.Join("\\N", output);
    }

    private static IReadOnlyList<string> ResolveColors(LyricStyleConfiguration config)
    {
        var start = config.TextColor;
        var middle = string.IsNullOrWhiteSpace(config.TextColor2) ? config.HighlightColor : config.TextColor2!;
        var end = string.IsNullOrWhiteSpace(config.TextColor3) ? config.HighlightColor : config.TextColor3!;
        return [start, middle, end];
    }

    private static string ApplyToLine(string line, IReadOnlyList<string> colors)
    {
        var units = SplitUnits(line);
        if (units.Count == 0)
        {
            return AssDocumentBuilder.EscapeAssText(line);
        }

        var sb = new StringBuilder();
        for (var i = 0; i < units.Count; i++)
        {
            var t = units.Count <= 1 ? 0 : (double)i / (units.Count - 1);
            var color = InterpolateColors(colors, t);
            sb.Append(CultureInfo.InvariantCulture, $"{{\\c{AssColorHelper.ToAssColor(color)}}}{AssDocumentBuilder.EscapeAssText(units[i])}");
        }

        return sb.ToString();
    }

    private static List<string> SplitUnits(string line)
    {
        var units = new List<string>();
        foreach (Match match in Regex.Matches(line, @"\S+|\s+"))
        {
            units.Add(match.Value);
        }

        return units;
    }

    private static string InterpolateColors(IReadOnlyList<string> colors, double t)
    {
        if (colors.Count == 1)
        {
            return colors[0];
        }

        if (colors.Count == 2)
        {
            return LerpHex(colors[0], colors[1], t);
        }

        return t <= 0.5
            ? LerpHex(colors[0], colors[1], t * 2)
            : LerpHex(colors[1], colors[2], (t - 0.5) * 2);
    }

    private static string LerpHex(string from, string to, double t)
    {
        t = Math.Clamp(t, 0, 1);
        var (r1, g1, b1) = ParseHex(from);
        var (r2, g2, b2) = ParseHex(to);
        var r = (byte)Math.Round(r1 + (r2 - r1) * t);
        var g = (byte)Math.Round(g1 + (g2 - g1) * t);
        var b = (byte)Math.Round(b1 + (b2 - b1) * t);
        return $"#{r:X2}{g:X2}{b:X2}";
    }

    private static (byte R, byte G, byte B) ParseHex(string hex)
    {
        var value = hex.TrimStart('#');
        if (value.Length != 6)
        {
            return (255, 255, 255);
        }

        return (
            byte.Parse(value[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture),
            byte.Parse(value[2..4], NumberStyles.HexNumber, CultureInfo.InvariantCulture),
            byte.Parse(value[4..6], NumberStyles.HexNumber, CultureInfo.InvariantCulture));
    }
}
