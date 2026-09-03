using System.Globalization;
using System.Text.RegularExpressions;
using Lyricsongs.Application.VideoPlan;
using Lyricsongs.Domain.Lyrics;
using Lyricsongs.Domain.Timeline;
using Lyricsongs.Domain.VideoPlan;

namespace Lyricsongs.Infrastructure.VideoPlan;

public sealed partial class VideoPlanValidator : IVideoPlanValidator
{
    private static readonly HashSet<int> AllowedFps = [24, 25, 30, 60];
    private static readonly HashSet<(int W, int H)> AllowedResolutions =
    [
        (1920, 1080),
        (1280, 720),
        (1080, 1920),
        (720, 1280),
        (1080, 1080)
    ];

    private static readonly Regex HexColorRegex = HexColorPattern();
    private const long TimelineToleranceMs = 2;

    public VideoPlanValidationResult Validate(
        Domain.VideoPlan.VideoPlan plan,
        IReadOnlyList<VideoPlanAsset> assets,
        VideoPlanRequest request)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(assets);
        ArgumentNullException.ThrowIfNull(request);

        var issues = new List<VideoPlanValidationIssue>();
        var assetById = assets.GroupBy(a => a.AssetId)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);

        ValidateAssets(assets, issues);
        ValidateOutput(plan.Output, issues);
        ValidateLyrics(plan.Lyrics, issues);
        ValidateTransition(plan.Transition, issues);
        ValidateBackgrounds(plan, assetById, issues);
        ValidateSubtitles(plan.Subtitles, plan.AudioDurationMs, issues);

        return new VideoPlanValidationResult { Issues = issues };
    }

    private static void ValidateAssets(IReadOnlyList<VideoPlanAsset> assets, List<VideoPlanValidationIssue> issues)
    {
        if (assets.Count == 0)
        {
            issues.Add(Error(VideoPlanValidationCode.NoAssets, "At least one background asset is required."));
            return;
        }

        foreach (var group in assets.GroupBy(a => a.AssetId).Where(g => g.Count() > 1))
        {
            issues.Add(Error(
                VideoPlanValidationCode.DuplicateAssetId,
                $"Duplicate asset id '{group.Key}'.",
                assetId: group.Key));
        }

        foreach (var asset in assets)
        {
            if (asset.Type == BackgroundAssetType.Video && asset.SourceDurationMs is null or <= 0)
            {
                issues.Add(Error(
                    VideoPlanValidationCode.MissingVideoDuration,
                    $"Video asset '{asset.AssetId}' requires a positive source duration.",
                    assetId: asset.AssetId));
            }
        }
    }

    private static void ValidateOutput(VideoOutputSettings output, List<VideoPlanValidationIssue> issues)
    {
        if (!AllowedResolutions.Contains((output.Width, output.Height)))
        {
            issues.Add(Error(
                VideoPlanValidationCode.InvalidResolution,
                $"Resolution {output.Width}x{output.Height} is not supported. Allowed: 1920x1080, 1280x720, 1080x1920, 720x1280, 1080x1080."));
        }

        if (!AllowedFps.Contains(output.Fps))
        {
            issues.Add(Error(
                VideoPlanValidationCode.InvalidFps,
                $"FPS {output.Fps} is not supported. Allowed: 24, 25, 30, 60."));
        }

        if (!string.Equals(output.VideoCodec, "libx264", StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(Error(
                VideoPlanValidationCode.InvalidCodec,
                $"Video codec '{output.VideoCodec}' is not supported. Only libx264 is allowed."));
        }

        if (!string.Equals(output.AudioCodec, "aac", StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(Error(
                VideoPlanValidationCode.InvalidCodec,
                $"Audio codec '{output.AudioCodec}' is not supported. Only aac is allowed."));
        }

        if (!string.Equals(output.Format, "mp4", StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(Error(
                VideoPlanValidationCode.InvalidCodec,
                $"Container format '{output.Format}' is not supported. Only mp4 is allowed."));
        }
    }

    private static void ValidateLyrics(LyricStyleSettings lyrics, List<VideoPlanValidationIssue> issues)
    {
        if (!Enum.IsDefined(lyrics.Style))
        {
            issues.Add(Error(
                VideoPlanValidationCode.InvalidLyricStyle,
                $"Lyric style '{lyrics.Style}' is not valid."));
        }

        if (!LyricStyleManager.IsAllowedFont(lyrics.Font))
        {
            issues.Add(Error(
                VideoPlanValidationCode.InvalidFont,
                $"Font '{lyrics.Font}' is not in the allow-list."));
        }

        if (lyrics.FontSize is < 12 or > 200)
        {
            issues.Add(Error(
                VideoPlanValidationCode.InvalidLyricStyle,
                $"Font size {lyrics.FontSize} is out of range (12–200)."));
        }

        ValidateColor(lyrics.TextColor, "textColor", issues);
        if (!string.IsNullOrWhiteSpace(lyrics.TextColor2))
        {
            ValidateColor(lyrics.TextColor2, "textColor2", issues);
        }

        if (!string.IsNullOrWhiteSpace(lyrics.TextColor3))
        {
            ValidateColor(lyrics.TextColor3, "textColor3", issues);
        }

        ValidateColor(lyrics.HighlightColor, "highlightColor", issues);
        ValidateColor(lyrics.OutlineColor, "outlineColor", issues);
        ValidateColor(lyrics.ShadowColor, "shadowColor", issues);
    }

    private static void ValidateColor(string color, string field, List<VideoPlanValidationIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(color) || !HexColorRegex.IsMatch(color))
        {
            issues.Add(Error(
                VideoPlanValidationCode.InvalidColor,
                $"Invalid {field} '{color}'. Expected #RRGGBB."));
        }
    }

    private static void ValidateTransition(TransitionSettings transition, List<VideoPlanValidationIssue> issues)
    {
        if (!Enum.IsDefined(transition.Type))
        {
            issues.Add(Error(
                VideoPlanValidationCode.InvalidTransition,
                $"Transition type '{transition.Type}' is not valid."));
        }

        if (transition.Type != TransitionType.None && transition.DurationMs is not (500 or 1000 or 1500))
        {
            // Allow other positive values but warn — preferred presets are 500/1000/1500
            if (transition.DurationMs <= 0)
            {
                issues.Add(Error(
                    VideoPlanValidationCode.InvalidTransition,
                    "Transition duration must be positive when transition is enabled."));
            }
            else if (transition.DurationMs is not (500 or 1000 or 1500) and > 0)
            {
                issues.Add(Warning(
                    VideoPlanValidationCode.InvalidTransition,
                    $"Transition duration {transition.DurationMs} ms is non-standard. Preferred: 500, 1000, or 1500 ms."));
            }
        }
    }

    private static void ValidateBackgrounds(
        Domain.VideoPlan.VideoPlan plan,
        IReadOnlyDictionary<string, VideoPlanAsset> assetById,
        List<VideoPlanValidationIssue> issues)
    {
        var backgrounds = plan.Backgrounds;
        if (backgrounds.Count == 0)
        {
            issues.Add(Error(VideoPlanValidationCode.NoAssets, "Video plan has no background segments."));
            return;
        }

        if (backgrounds[0].StartMs != 0)
        {
            issues.Add(Error(
                VideoPlanValidationCode.TimelineDoesNotStartAtZero,
                $"Timeline must start at 0 but starts at {backgrounds[0].StartMs} ms.",
                segmentIndex: 0));
        }

        for (var i = 0; i < backgrounds.Count; i++)
        {
            var segment = backgrounds[i];

            if (!assetById.TryGetValue(segment.AssetId, out var asset))
            {
                issues.Add(Error(
                    VideoPlanValidationCode.UnknownAssetId,
                    $"Background segment references unknown asset id '{segment.AssetId}'.",
                    assetId: segment.AssetId,
                    segmentIndex: i));
                continue;
            }

            if (segment.Type != asset.Type)
            {
                issues.Add(Error(
                    VideoPlanValidationCode.AssetTypeMismatch,
                    $"Segment type '{segment.Type}' does not match uploaded asset type '{asset.Type}' for '{segment.AssetId}'.",
                    assetId: segment.AssetId,
                    segmentIndex: i));
            }

            if (segment.EndMs <= segment.StartMs)
            {
                issues.Add(Error(
                    VideoPlanValidationCode.InvalidSegmentRange,
                    $"Segment {i} for '{segment.AssetId}' has end <= start.",
                    assetId: segment.AssetId,
                    segmentIndex: i));
            }

            if (!Enum.IsDefined(segment.Animation))
            {
                issues.Add(Error(
                    VideoPlanValidationCode.InvalidAnimation,
                    $"Invalid animation '{segment.Animation}' on segment {i}.",
                    assetId: segment.AssetId,
                    segmentIndex: i));
            }

            if (segment.Type == BackgroundAssetType.Video && segment.Animation != BackgroundAnimation.None)
            {
                issues.Add(Warning(
                    VideoPlanValidationCode.InvalidAnimation,
                    $"Video segment '{segment.AssetId}' has animation '{segment.Animation}'; videos should use None.",
                    assetId: segment.AssetId,
                    segmentIndex: i));
            }

            if (segment.TransitionOut != TransitionType.None &&
                segment.TransitionOutDurationMs > segment.DurationMs)
            {
                issues.Add(Error(
                    VideoPlanValidationCode.TransitionExceedsSegment,
                    $"Transition out of segment {i} ({segment.TransitionOutDurationMs} ms) exceeds segment duration ({segment.DurationMs} ms).",
                    assetId: segment.AssetId,
                    segmentIndex: i));
            }

            if (i > 0)
            {
                var prev = backgrounds[i - 1];
                var expectedStart = prev.EndMs - prev.TransitionOutDurationMs;
                var delta = segment.StartMs - expectedStart;

                if (delta > TimelineToleranceMs)
                {
                    issues.Add(Error(
                        VideoPlanValidationCode.TimelineGap,
                        $"Gap of {delta} ms between segment {i - 1} and {i}.",
                        segmentIndex: i));
                }
                else if (delta < -TimelineToleranceMs)
                {
                    issues.Add(Error(
                        VideoPlanValidationCode.TimelineOverlap,
                        $"Unexpected overlap of {-delta} ms between segment {i - 1} and {i} beyond transition.",
                        segmentIndex: i));
                }
            }
        }

        var last = backgrounds[^1];
        var endDelta = last.EndMs - plan.AudioDurationMs;
        if (endDelta > TimelineToleranceMs)
        {
            issues.Add(Error(
                VideoPlanValidationCode.TimelineOverflow,
                $"Timeline exceeds audio duration by {endDelta} ms.",
                segmentIndex: backgrounds.Count - 1));
        }
        else if (endDelta < -TimelineToleranceMs)
        {
            issues.Add(Error(
                VideoPlanValidationCode.TimelineUnderrun,
                $"Timeline ends {Math.Abs(endDelta)} ms before audio duration.",
                segmentIndex: backgrounds.Count - 1));
        }

        for (var i = 1; i < backgrounds.Count; i++)
        {
            if (string.Equals(backgrounds[i - 1].AssetId, backgrounds[i].AssetId, StringComparison.Ordinal))
            {
                issues.Add(Warning(
                    VideoPlanValidationCode.AdjacentDuplicateAsset,
                    $"Asset '{backgrounds[i].AssetId}' appears in consecutive segments {i - 1} and {i}.",
                    assetId: backgrounds[i].AssetId,
                    segmentIndex: i));
            }
        }
    }

    private static void ValidateSubtitles(
        IReadOnlyList<Domain.Subtitles.LyricCue> subtitles,
        long audioDurationMs,
        List<VideoPlanValidationIssue> issues)
    {
        foreach (var cue in subtitles)
        {
            if (cue.StartMs < 0 || cue.EndMs < 0)
            {
                issues.Add(Error(
                    VideoPlanValidationCode.SubtitleNegativeTimestamp,
                    $"Subtitle {cue.Index} has a negative timestamp.",
                    subtitleIndex: cue.Index));
            }

            if (cue.EndMs <= cue.StartMs)
            {
                issues.Add(Error(
                    VideoPlanValidationCode.SubtitleInvalidRange,
                    $"Subtitle {cue.Index} has end <= start.",
                    subtitleIndex: cue.Index));
            }

            if (cue.EndMs > audioDurationMs + TimelineToleranceMs)
            {
                issues.Add(Error(
                    VideoPlanValidationCode.SubtitleExceedsAudio,
                    $"Subtitle {cue.Index} ends at {cue.EndMs} ms which exceeds audio duration {audioDurationMs} ms.",
                    subtitleIndex: cue.Index));
            }

            if (string.IsNullOrWhiteSpace(cue.Text))
            {
                issues.Add(Error(
                    VideoPlanValidationCode.SubtitleEmptyText,
                    $"Subtitle {cue.Index} has empty text.",
                    subtitleIndex: cue.Index));
            }
        }
    }

    private static VideoPlanValidationIssue Error(
        string code,
        string message,
        string? assetId = null,
        int? segmentIndex = null,
        int? subtitleIndex = null) => new()
    {
        Code = code,
        Message = message,
        Severity = VideoPlanValidationSeverity.Error,
        AssetId = assetId,
        SegmentIndex = segmentIndex,
        SubtitleIndex = subtitleIndex
    };

    private static VideoPlanValidationIssue Warning(
        string code,
        string message,
        string? assetId = null,
        int? segmentIndex = null) => new()
    {
        Code = code,
        Message = message,
        Severity = VideoPlanValidationSeverity.Warning,
        AssetId = assetId,
        SegmentIndex = segmentIndex
    };

    [GeneratedRegex(@"^#[0-9A-Fa-f]{6}$", RegexOptions.CultureInvariant)]
    private static partial Regex HexColorPattern();
}
