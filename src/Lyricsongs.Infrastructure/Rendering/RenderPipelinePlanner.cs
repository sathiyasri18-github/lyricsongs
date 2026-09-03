using System.Globalization;
using System.Text;
using Lyricsongs.Application.Rendering;
using Lyricsongs.Domain.Rendering;
using Lyricsongs.Domain.Timeline;

namespace Lyricsongs.Infrastructure.Rendering;

public sealed class RenderPipelinePlanner : IRenderPipelinePlanner
{
    public RenderPipelinePlan Plan(VideoRenderRequest request, string workingDirectory)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(workingDirectory);

        var output = request.Output;
        var segments = new List<SegmentRenderPlan>();

        foreach (var timelineSegment in request.Timeline.Segments)
        {
            if (!request.AssetPathMap.TryGetValue(timelineSegment.AssetId, out var assetPath))
            {
                throw new InvalidOperationException($"Asset path not found for id '{timelineSegment.AssetId}'.");
            }

            var motion = ResolveKenBurnsMotion(timelineSegment.Sequence, output);
            var segmentPath = System.IO.Path.Combine(workingDirectory, $"seg_{timelineSegment.Sequence:D3}.mp4");

            segments.Add(new SegmentRenderPlan
            {
                Sequence = timelineSegment.Sequence,
                SourceAssetPath = assetPath,
                OutputSegmentPath = segmentPath,
                DurationSeconds = timelineSegment.SlotDurationMs / 1000.0,
                IsImage = timelineSegment.AssetType == BackgroundAssetType.Image,
                KenBurnsMotion = motion,
                LoopCount = Math.Max(1, timelineSegment.LoopCount),
                TrimStartSeconds = timelineSegment.TrimStartMs / 1000.0,
                TransitionOutDurationMs = timelineSegment.TransitionOutDurationMs
            });
        }

        return new RenderPipelinePlan
        {
            WorkingDirectory = workingDirectory,
            AssFilePath = System.IO.Path.Combine(workingDirectory, "lyrics.ass"),
            BackgroundVideoPath = System.IO.Path.Combine(workingDirectory, "background.mp4"),
            OutputFilePath = request.OutputFilePath ?? System.IO.Path.Combine(workingDirectory, "output.mp4"),
            Segments = segments,
            TotalDurationMs = request.Timeline.AudioDurationMs
        };
    }

    private static KenBurnsMotion ResolveKenBurnsMotion(int sequence, RenderOutputConfiguration output)
    {
        if (!output.CycleKenBurnsMotions)
        {
            return output.DefaultKenBurnsMotion;
        }

        var motions = new[] { KenBurnsMotion.ZoomIn, KenBurnsMotion.ZoomOut, KenBurnsMotion.PanLeft, KenBurnsMotion.PanRight };
        return motions[(sequence - 1) % motions.Length];
    }
}
