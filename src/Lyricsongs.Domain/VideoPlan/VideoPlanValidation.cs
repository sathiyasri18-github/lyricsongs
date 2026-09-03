namespace Lyricsongs.Domain.VideoPlan;

public enum VideoPlanValidationSeverity
{
    Warning = 0,
    Error = 1
}

public static class VideoPlanValidationCode
{
    public const string InvalidAudioDuration = "INVALID_AUDIO_DURATION";
    public const string NoAssets = "NO_ASSETS";
    public const string UnknownAssetId = "UNKNOWN_ASSET_ID";
    public const string AssetTypeMismatch = "ASSET_TYPE_MISMATCH";
    public const string MissingVideoDuration = "MISSING_VIDEO_DURATION";
    public const string DuplicateAssetId = "DUPLICATE_ASSET_ID";
    public const string TimelineGap = "TIMELINE_GAP";
    public const string TimelineOverlap = "TIMELINE_OVERLAP";
    public const string TimelineDoesNotStartAtZero = "TIMELINE_NOT_START_ZERO";
    public const string TimelineUnderrun = "TIMELINE_UNDERRUN";
    public const string TimelineOverflow = "TIMELINE_OVERFLOW";
    public const string InvalidSegmentRange = "INVALID_SEGMENT_RANGE";
    public const string TransitionExceedsSegment = "TRANSITION_EXCEEDS_SEGMENT";
    public const string InvalidTransition = "INVALID_TRANSITION";
    public const string InvalidAnimation = "INVALID_ANIMATION";
    public const string InvalidResolution = "INVALID_RESOLUTION";
    public const string InvalidFps = "INVALID_FPS";
    public const string InvalidCodec = "INVALID_CODEC";
    public const string InvalidLyricStyle = "INVALID_LYRIC_STYLE";
    public const string InvalidFont = "INVALID_FONT";
    public const string InvalidColor = "INVALID_COLOR";
    public const string SubtitleNegativeTimestamp = "SUBTITLE_NEGATIVE_TIMESTAMP";
    public const string SubtitleInvalidRange = "SUBTITLE_INVALID_RANGE";
    public const string SubtitleExceedsAudio = "SUBTITLE_EXCEEDS_AUDIO";
    public const string SubtitleEmptyText = "SUBTITLE_EMPTY_TEXT";
    public const string AdjacentDuplicateAsset = "ADJACENT_DUPLICATE_ASSET";
}

public sealed class VideoPlanValidationIssue
{
    public required string Code { get; init; }
    public required string Message { get; init; }
    public required VideoPlanValidationSeverity Severity { get; init; }
    public string? AssetId { get; init; }
    public int? SegmentIndex { get; init; }
    public int? SubtitleIndex { get; init; }
}

public sealed class VideoPlanValidationResult
{
    public required IReadOnlyList<VideoPlanValidationIssue> Issues { get; init; }

    public bool IsValid => Issues.All(i => i.Severity != VideoPlanValidationSeverity.Error);
    public bool HasWarnings => Issues.Any(i => i.Severity == VideoPlanValidationSeverity.Warning);

    public IEnumerable<VideoPlanValidationIssue> Errors =>
        Issues.Where(i => i.Severity == VideoPlanValidationSeverity.Error);

    public IEnumerable<VideoPlanValidationIssue> Warnings =>
        Issues.Where(i => i.Severity == VideoPlanValidationSeverity.Warning);
}
