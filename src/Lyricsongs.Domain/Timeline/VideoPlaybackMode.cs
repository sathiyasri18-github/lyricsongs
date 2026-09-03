namespace Lyricsongs.Domain.Timeline;

public enum VideoPlaybackMode
{
    /// <summary>Video source duration matches the assigned segment slot.</summary>
    Normal = 0,

    /// <summary>Video is trimmed to fit the assigned segment slot.</summary>
    Trimmed = 1,

    /// <summary>Video is looped to fill the assigned segment slot.</summary>
    Looped = 2
}
