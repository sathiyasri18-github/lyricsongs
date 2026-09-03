using Lyricsongs.Domain.Timeline;

namespace Lyricsongs.Application.Timeline;

/// <summary>
/// Validates a generated background timeline for gaps, overflows, and transition constraints.
/// </summary>
public interface IBackgroundTimelineValidationService
{
    TimelineValidationResult Validate(BackgroundTimeline timeline, BackgroundTimelineRequest request);
}
