using System.Diagnostics;
using Lyricsongs.Application.Rendering;
using Microsoft.Extensions.Logging;

namespace Lyricsongs.Infrastructure.Rendering;

internal static class FfmpegHardwareEncoderSelector
{
    private static readonly string[] GpuEncoderPreference =
    [
        "h264_nvenc",
        "h264_amf",
        "h264_qsv",
        "h264_mf"
    ];

    public static void Select(FfmpegOptions options, ILogger logger)
    {
        var mode = (options.HardwareEncoding ?? "Auto").Trim();

        if (string.Equals(mode, "Cpu", StringComparison.OrdinalIgnoreCase))
        {
            Apply(options, "libx264", hardware: false);
            logger.LogInformation("FFmpeg video encoder: libx264 (CPU, forced)");
            return;
        }

        if (!string.IsNullOrWhiteSpace(options.VideoEncoder))
        {
            var forced = options.VideoEncoder.Trim();
            if (ProbeEncoder(options.ExecutablePath, forced))
            {
                Apply(options, forced, IsGpuEncoder(forced));
                logger.LogInformation("FFmpeg video encoder: {Encoder} (forced)", forced);
                return;
            }

            if (string.Equals(mode, "Gpu", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Configured FFmpeg encoder '{forced}' is not available on this system.");
            }

            logger.LogWarning(
                "Configured FFmpeg encoder '{Encoder}' is unavailable; falling back to auto-detection",
                forced);
        }

        foreach (var encoder in GpuEncoderPreference)
        {
            if (!ProbeEncoder(options.ExecutablePath, encoder))
            {
                continue;
            }

            Apply(options, encoder, hardware: true);
            logger.LogInformation("FFmpeg video encoder: {Encoder} (GPU)", encoder);
            return;
        }

        if (string.Equals(mode, "Gpu", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "HardwareEncoding=Gpu but no working GPU H.264 encoder was found (nvenc/amf/qsv/mf).");
        }

        Apply(options, "libx264", hardware: false);
        logger.LogInformation("FFmpeg video encoder: libx264 (CPU fallback — no GPU encoder available)");
    }

    private static void Apply(FfmpegOptions options, string encoder, bool hardware)
    {
        options.ResolvedVideoEncoder = encoder;
        options.IsHardwareEncoder = hardware;
        options.VideoEncoder = encoder;
    }

    private static bool IsGpuEncoder(string encoder) =>
        !string.Equals(encoder, "libx264", StringComparison.OrdinalIgnoreCase);

    private static bool ProbeEncoder(string ffmpegPath, string encoder)
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"lyricsongs-enc-{Guid.NewGuid():N}.mp4");
        try
        {
            var args = BuildProbeArgs(encoder, outputPath);
            var psi = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            foreach (var arg in args)
            {
                psi.ArgumentList.Add(arg);
            }

            using var process = Process.Start(psi);
            if (process is null)
            {
                return false;
            }

            if (!process.WaitForExit(8000))
            {
                try { process.Kill(entireProcessTree: true); } catch { /* ignore */ }
                return false;
            }

            return process.ExitCode == 0 && File.Exists(outputPath) && new FileInfo(outputPath).Length > 0;
        }
        catch
        {
            return false;
        }
        finally
        {
            try
            {
                if (File.Exists(outputPath))
                {
                    File.Delete(outputPath);
                }
            }
            catch
            {
                /* ignore */
            }
        }
    }

    private static List<string> BuildProbeArgs(string encoder, string outputPath)
    {
        var args = new List<string>
        {
            "-hide_banner", "-y",
            "-f", "lavfi",
            "-i", "color=c=black:s=640x360:d=0.2",
            "-frames:v", "3",
            "-c:v", encoder
        };

        args.AddRange(FfmpegVideoEncodeArgs.BuildQualityArgs(encoder, videoCrf: 23, videoPreset: "medium"));
        args.AddRange(["-an", "-pix_fmt", FfmpegVideoEncodeArgs.PixelFormatFor(encoder), outputPath]);
        return args;
    }
}
