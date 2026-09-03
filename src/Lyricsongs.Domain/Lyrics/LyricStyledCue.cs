namespace Lyricsongs.Domain.Lyrics;

/// <summary>
/// A lyric cue enriched with ASS override tags for the selected animation style.
/// </summary>
public sealed class LyricStyledCue
{
    public required int Index { get; init; }
    public required long StartMs { get; init; }
    public required long EndMs { get; init; }
    public required string Text { get; init; }
    public required string AssOverrideTags { get; init; }
    public required string AssDialogueText { get; init; }
}
