using System.Globalization;
using System.Text.RegularExpressions;
using Lyricsongs.Application.Rendering;

namespace Lyricsongs.Infrastructure.Rendering;

public sealed partial class FfmpegProgressParser : IFfmpegProgressParser
{
    private static readonly Regex TimeRegex = TimePattern();
    private static readonly Regex FrameRegex = FramePattern();

    public FfmpegProgressUpdate? ParseProgressLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line) || !line.Contains('='))
        {
            return ParseStatsLine(line);
        }

        var parts = line.Split('=', 2);
        if (parts.Length != 2)
        {
            return null;
        }

        var key = parts[0].Trim();
        var value = parts[1].Trim();

        return key switch
        {
            "out_time_ms" when long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var micro) =>
                new FfmpegProgressUpdate { ProcessedDurationMs = micro / 1000, RawLine = line },
            "out_time" => ParseOutTime(value, line),
            "frame" when long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var frame) =>
                new FfmpegProgressUpdate { Frame = frame, RawLine = line },
            _ => null
        };
    }

    public FfmpegProgressUpdate? ParseStatsLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return null;
        }

        long? durationMs = null;
        long? frame = null;
        double? speed = null;

        var timeMatch = TimeRegex.Match(line);
        if (timeMatch.Success)
        {
            durationMs = ParseTimecodeToMs(timeMatch.Groups[1].Value);
        }

        var frameMatch = FrameRegex.Match(line);
        if (frameMatch.Success && long.TryParse(frameMatch.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var f))
        {
            frame = f;
        }

        var speedMatch = Regex.Match(line, @"speed=\s*([0-9.]+)x", RegexOptions.CultureInvariant);
        if (speedMatch.Success && double.TryParse(speedMatch.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var s))
        {
            speed = s;
        }

        if (durationMs is null && frame is null && speed is null)
        {
            return null;
        }

        return new FfmpegProgressUpdate
        {
            ProcessedDurationMs = durationMs,
            Frame = frame,
            Speed = speed,
            RawLine = line
        };
    }

    private static FfmpegProgressUpdate? ParseOutTime(string value, string rawLine)
    {
        var ms = ParseTimecodeToMs(value);
        return ms is null ? null : new FfmpegProgressUpdate { ProcessedDurationMs = ms, RawLine = rawLine };
    }

    private static long? ParseTimecodeToMs(string value)
    {
        var parts = value.Split(':');
        if (parts.Length != 3)
        {
            return null;
        }

        if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var hours) ||
            !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var minutes))
        {
            return null;
        }

        if (!double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds))
        {
            return null;
        }

        return (long)((hours * 3600 + minutes * 60 + seconds) * 1000);
    }

    [GeneratedRegex(@"time=\s*(\d{2}:\d{2}:\d{2}\.\d{2})", RegexOptions.CultureInvariant)]
    private static partial Regex TimePattern();

    [GeneratedRegex(@"frame=\s*(\d+)", RegexOptions.CultureInvariant)]
    private static partial Regex FramePattern();
}
