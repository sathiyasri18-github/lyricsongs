namespace Lyricsongs.Domain.Subtitles;

public static class SrtValidationCode
{
    public const string EmptyFile = "EMPTY_FILE";
    public const string MissingTimestamp = "MISSING_TIMESTAMP";
    public const string InvalidTimecode = "INVALID_TIMECODE";
    public const string InvalidTimeRange = "INVALID_TIME_RANGE";
    public const string MissingText = "MISSING_TEXT";
    public const string MissingIndex = "MISSING_INDEX";
    public const string InvalidIndex = "INVALID_INDEX";
    public const string MalformedBlock = "MALFORMED_BLOCK";
    public const string OverlappingCues = "OVERLAPPING_CUES";
    public const string DuplicateIndex = "DUPLICATE_INDEX";
    public const string ZeroDuration = "ZERO_DURATION";
    public const string LargeGap = "LARGE_GAP";
}
