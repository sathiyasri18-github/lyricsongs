namespace Lyricsongs.Domain.Rendering;

public enum RenderStage
{
    Preparing = 0,
    WritingSubtitles = 1,
    RenderingSegments = 2,
    CompositingBackground = 3,
    OverlayingLyrics = 4,
    MuxingAudio = 5,
    Finalizing = 6,
    Completed = 7,
    Failed = 8,
    Cancelled = 9
}
