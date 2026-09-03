using System.Globalization;
using System.Text;
using Lyricsongs.Domain.Lyrics;

namespace Lyricsongs.Infrastructure.Lyrics.Ass;

internal static class KaraokeTextBuilder
{
    /// <summary>
    /// Builds ASS karaoke text with \kf tags. Durations are in centiseconds.
    /// Supports Tamil/Unicode via grapheme cluster splitting.
    /// </summary>
    public static string Build(string text, long durationMs, KaraokeSplitMode splitMode)
    {
        var units = splitMode == KaraokeSplitMode.Word
            ? SplitWords(text)
            : SplitGraphemes(text);

        if (units.Count == 0)
        {
            return AssDocumentBuilder.EscapeAssText(text);
        }

        var totalCs = Math.Max(1, durationMs / 10);
        var baseCs = totalCs / units.Count;
        var remainder = totalCs - (baseCs * units.Count);

        var sb = new StringBuilder();
        for (var i = 0; i < units.Count; i++)
        {
            var cs = baseCs + (i < remainder ? 1 : 0);
            sb.Append(CultureInfo.InvariantCulture, $"{{\\kf{cs}}}{units[i]}");
        }

        return sb.ToString();
    }

    private static List<string> SplitWords(string text)
    {
        var lines = text.Split('\n');
        var result = new List<string>();

        for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            var line = lines[lineIndex].Trim();
            if (line.Length == 0)
            {
                continue;
            }

            var words = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < words.Length; i++)
            {
                result.Add(words[i]);
                if (i < words.Length - 1)
                {
                    result.Add(" ");
                }
            }

            if (lineIndex < lines.Length - 1)
            {
                result.Add("\\N");
            }
        }

        return result;
    }

    private static List<string> SplitGraphemes(string text)
    {
        var result = new List<string>();
        var enumerator = System.Globalization.StringInfo.GetTextElementEnumerator(text);

        while (enumerator.MoveNext())
        {
            var element = enumerator.GetTextElement();
            result.Add(element == "\n" ? "\\N" : element);
        }

        return result;
    }
}
