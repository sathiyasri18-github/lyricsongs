using System.Text;

namespace Lyricsongs.Infrastructure.Rendering;

/// <summary>
/// Cross-platform path escaping for FFmpeg filter arguments.
/// </summary>
public static class FfmpegPathHelper
{
    public static string EscapeFilterPath(string path)
    {
        var normalized = path.Replace('\\', '/');
        var sb = new StringBuilder(normalized.Length + 8);

        foreach (var ch in normalized)
        {
            switch (ch)
            {
                case ':':
                case '\'':
                case ',':
                case ';':
                case '[':
                case ']':
                case ' ':
                    sb.Append('\\');
                    sb.Append(ch);
                    break;
                default:
                    sb.Append(ch);
                    break;
            }
        }

        return sb.ToString();
    }

    public static string QuoteArgument(string value)
    {
        if (OperatingSystem.IsWindows())
        {
            return value.Contains(' ') ? $"\"{value}\"" : value;
        }

        return value.Contains(' ') ? $"'{value.Replace("'", "'\\''")}'" : value;
    }

    public static string ToCommandLine(string executable, IEnumerable<string> arguments) =>
        $"{QuoteArgument(executable)} {string.Join(' ', arguments.Select(QuoteArgument))}";
}
