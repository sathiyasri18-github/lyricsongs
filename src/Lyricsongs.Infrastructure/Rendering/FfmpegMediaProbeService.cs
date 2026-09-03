using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using Lyricsongs.Application.Rendering;
using Microsoft.Extensions.Options;

namespace Lyricsongs.Infrastructure.Rendering;

public sealed partial class FfmpegMediaProbeService : IMediaProbeService
{
    private readonly FfmpegOptions _options;

    public FfmpegMediaProbeService(IOptions<FfmpegOptions> options)
    {
        _options = options.Value;
    }

    public async Task<long?> ProbeDurationMsAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            return null;
        }

        var probePath = string.IsNullOrWhiteSpace(_options.FFprobeExecutablePath)
            ? "ffprobe"
            : _options.FFprobeExecutablePath;

        var psi = new ProcessStartInfo
        {
            FileName = probePath,
            Arguments = $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 \"{filePath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            using var process = Process.Start(psi);
            if (process is null)
            {
                return null;
            }

            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);
            if (process.ExitCode != 0)
            {
                return null;
            }

            var trimmed = output.Trim();
            if (double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds))
            {
                return (long)Math.Round(seconds * 1000);
            }

            var match = DurationRegex().Match(trimmed);
            if (match.Success &&
                double.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out seconds))
            {
                return (long)Math.Round(seconds * 1000);
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    [GeneratedRegex(@"(\d+(?:\.\d+)?)")]
    private static partial Regex DurationRegex();
}
