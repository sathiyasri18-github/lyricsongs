namespace Lyricsongs.Domain.Timeline;

public static class TimelineValidationCode
{
    public const string InvalidAudioDuration = "INVALID_AUDIO_DURATION";
    public const string NoAssets = "NO_ASSETS";
    public const string DuplicateAssetId = "DUPLICATE_ASSET_ID";
    public const string MissingVideoDuration = "MISSING_VIDEO_DURATION";
    public const string InvalidTransitionDuration = "INVALID_TRANSITION_DURATION";
    public const string TransitionExceedsSegment = "TRANSITION_EXCEEDS_SEGMENT";
    public const string TimelineGap = "TIMELINE_GAP";
    public const string TimelineOverflow = "TIMELINE_OVERFLOW";
    public const string TimelineUnderrun = "TIMELINE_UNDERRUN";
    public const string AdjacentDuplicateImage = "ADJACENT_DUPLICATE_IMAGE";
    public const string InvalidSegmentDuration = "INVALID_SEGMENT_DURATION";
    public const string MissingSegment = "MISSING_SEGMENT";
}
