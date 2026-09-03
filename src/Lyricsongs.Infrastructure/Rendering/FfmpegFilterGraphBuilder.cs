using System.Globalization;
using System.Text;
using Lyricsongs.Application.Rendering;
using Lyricsongs.Domain.Rendering;

namespace Lyricsongs.Infrastructure.Rendering;

public sealed class FfmpegFilterGraphBuilder : IFfmpegFilterGraphBuilder
{
    public string BuildKenBurnsFilter(KenBurnsMotion motion, int width, int height, int frameRate, int frameCount)
    {
        var (zoomExpr, xExpr, yExpr) = motion switch
        {
            KenBurnsMotion.ZoomIn => (
                "if(lte(zoom,1.0),1.0+on*0.0006,zoom)",
                "iw/2-(iw/zoom/2)",
                "ih/2-(ih/zoom/2)"),
            KenBurnsMotion.ZoomOut => (
                "if(gte(zoom,1.15),1.15-on*0.0006,zoom)",
                "iw/2-(iw/zoom/2)",
                "ih/2-(ih/zoom/2)"),
            KenBurnsMotion.PanLeft => (
                "1.15",
                "on*0.6",
                "ih/2-(ih/zoom/2)"),
            KenBurnsMotion.PanRight => (
                "1.15",
                "(iw-iw/zoom)-on*0.6",
                "ih/2-(ih/zoom/2)"),
            _ => ("1.1", "iw/2-(iw/zoom/2)", "ih/2-(ih/zoom/2)")
        };

        return string.Create(CultureInfo.InvariantCulture,
            $"scale={width}:{height}:force_original_aspect_ratio=increase," +
            $"crop={width}:{height}," +
            $"zoompan=z='{zoomExpr}':x='{xExpr}':y='{yExpr}':d={frameCount}:s={width}x{height}:fps={frameRate}");
    }

    public string BuildScalePadFilter(int width, int height, int frameRate) =>
        $"scale={width}:{height}:force_original_aspect_ratio=decrease," +
        $"pad={width}:{height}:(ow-iw)/2:(oh-ih)/2:color=black," +
        $"setsar=1,fps={frameRate}";

    public string BuildAssOverlayFilter(string assFilePath)
    {
        var escaped = FfmpegPathHelper.EscapeFilterPath(assFilePath);
        return $"ass='{escaped}'";
    }

    public string BuildXfadeFilterComplex(
        IReadOnlyList<double> segmentDurationsSec,
        IReadOnlyList<double> transitionDurationsSec,
        string transitionName)
    {
        if (segmentDurationsSec.Count == 0)
        {
            throw new ArgumentException("At least one segment is required.", nameof(segmentDurationsSec));
        }

        if (segmentDurationsSec.Count == 1)
        {
            return "[0:v]copy[vout]";
        }

        var sb = new StringBuilder();
        var currentLabel = "0:v";
        var cumulativeDuration = segmentDurationsSec[0];

        for (var i = 0; i < transitionDurationsSec.Count; i++)
        {
            var transitionSec = transitionDurationsSec[i];
            var offset = Math.Max(0, cumulativeDuration - transitionSec);
            var nextInput = $"{i + 1}:v";
            var outLabel = i == transitionDurationsSec.Count - 1 ? "vout" : $"v{i + 1}";

            sb.Append(CultureInfo.InvariantCulture,
                $"[{currentLabel}][{nextInput}]xfade=transition={transitionName}:duration={transitionSec:F3}:offset={offset:F3}[{outLabel}]");

            if (i < transitionDurationsSec.Count - 1)
            {
                sb.Append(';');
            }

            cumulativeDuration = offset + segmentDurationsSec[i + 1];
            currentLabel = outLabel;
        }

        return sb.ToString();
    }
}
