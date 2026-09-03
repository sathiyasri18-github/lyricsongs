using System.Globalization;

namespace Lyricsongs.Infrastructure.Rendering;

internal static class FfmpegVideoEncodeArgs
{
    public static IEnumerable<string> Build(string videoCodec, string videoPreset, int videoCrf)
    {
        yield return "-c:v";
        yield return videoCodec;

        foreach (var arg in BuildQualityArgs(videoCodec, videoCrf, videoPreset))
        {
            yield return arg;
        }

        yield return "-pix_fmt";
        yield return PixelFormatFor(videoCodec);
    }

    public static IEnumerable<string> BuildQualityArgs(string videoCodec, int videoCrf, string videoPreset)
    {
        switch (videoCodec.ToLowerInvariant())
        {
            case "h264_nvenc":
                yield return "-preset";
                yield return MapNvencPreset(videoPreset);
                yield return "-rc";
                yield return "constqp";
                yield return "-cq";
                yield return Clamp(videoCrf, 0, 51).ToString(CultureInfo.InvariantCulture);
                yield break;

            case "h264_amf":
                yield return "-quality";
                yield return "quality";
                yield return "-rc";
                yield return "cqp";
                yield return "-qp_i";
                yield return Clamp(videoCrf, 0, 51).ToString(CultureInfo.InvariantCulture);
                yield return "-qp_p";
                yield return Clamp(videoCrf, 0, 51).ToString(CultureInfo.InvariantCulture);
                yield break;

            case "h264_qsv":
                yield return "-global_quality";
                yield return Clamp(videoCrf, 1, 51).ToString(CultureInfo.InvariantCulture);
                yield break;

            case "h264_mf":
                yield return "-rate_control";
                yield return "quality";
                yield return "-quality";
                yield return MapMfQuality(videoCrf).ToString(CultureInfo.InvariantCulture);
                yield break;

            default:
                yield return "-preset";
                yield return string.IsNullOrWhiteSpace(videoPreset) ? "medium" : videoPreset;
                yield return "-crf";
                yield return Clamp(videoCrf, 0, 51).ToString(CultureInfo.InvariantCulture);
                yield break;
        }
    }

    public static string PixelFormatFor(string videoCodec) =>
        string.Equals(videoCodec, "h264_mf", StringComparison.OrdinalIgnoreCase)
            ? "nv12"
            : "yuv420p";

    private static string MapNvencPreset(string preset) => preset.ToLowerInvariant() switch
    {
        "ultrafast" or "superfast" or "veryfast" => "p1",
        "faster" => "p2",
        "fast" => "p3",
        "medium" => "p4",
        "slow" => "p5",
        "slower" => "p6",
        "veryslow" => "p7",
        _ when preset.StartsWith('p') => preset,
        _ => "p4"
    };

    /// <summary>Map libx264 CRF (lower=better) to Media Foundation quality (higher=better).</summary>
    private static int MapMfQuality(int crf) =>
        Clamp(100 - (crf * 2), 1, 100);

    private static int Clamp(int value, int min, int max) =>
        Math.Min(max, Math.Max(min, value));
}
