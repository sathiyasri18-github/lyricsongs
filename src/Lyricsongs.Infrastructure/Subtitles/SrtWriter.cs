using System.Text;
using Lyricsongs.Domain.Subtitles;

namespace Lyricsongs.Infrastructure.Subtitles;

public static class SrtWriter
{
    public static string Serialize(IReadOnlyList<LyricCue> lyrics)
    {
        var ordered = lyrics
            .OrderBy(c => c.StartMs)
            .ThenBy(c => c.Index)
            .ToList();

        var sb = new StringBuilder();
        for (var i = 0; i < ordered.Count; i++)
        {
            var cue = ordered[i];
            var index = i + 1;
            var start = new Timecode(cue.StartMs).ToSrtString();
            var end = new Timecode(cue.EndMs).ToSrtString();

            sb.AppendLine(index.ToString());
            sb.AppendLine($"{start} --> {end}");
            sb.Append(CueText(cue.Text));
            sb.AppendLine();
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static string CueText(string text) =>
        text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
}
