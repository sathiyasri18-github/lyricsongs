using Lyricsongs.Domain.Subtitles;
using Lyricsongs.Domain.VideoPlan;

namespace Lyricsongs.Application.Projects;

public interface IProjectPlanService
{
    Task<ProjectPlanResponse> BuildAsync(string projectId, VideoPlanSettings? settingsOverride = null, CancellationToken cancellationToken = default);
}

public sealed class ProjectPlanResponse
{
    public required string ProjectId { get; init; }
    public required string PlanId { get; init; }
    public required long AudioDurationMs { get; init; }
    public required bool IsValid { get; init; }
    public required IReadOnlyList<ProjectPlanTimelineSegmentDto> Timeline { get; init; }
    public required IReadOnlyList<LyricCue> Lyrics { get; init; }
    public required ProjectPlanOutputDto Output { get; init; }
    public required ProjectPlanLyricsDto LyricsStyle { get; init; }
    public required IReadOnlyList<string> ValidationMessages { get; init; }
}

public sealed class ProjectPlanTimelineSegmentDto
{
    public required string AssetId { get; init; }
    public required string FileName { get; init; }
    public required string Type { get; init; }
    public required long StartMs { get; init; }
    public required long EndMs { get; init; }
    public required string Animation { get; init; }
    public required string TransitionOut { get; init; }
    public required long TransitionOutDurationMs { get; init; }
}

public sealed class ProjectPlanOutputDto
{
    public int Width { get; init; }
    public int Height { get; init; }
    public int Fps { get; init; }
    public string VideoCodec { get; init; } = "libx264";
    public string AudioCodec { get; init; } = "aac";
    public string Format { get; init; } = "mp4";
    public string AspectRatio { get; init; } = "Landscape16x9";
}

public sealed class ProjectPlanLyricsDto
{
    public required string Style { get; init; }
    public required string Font { get; init; }
    public int FontSize { get; init; }
    public required string Position { get; init; }
    public required string TextColor { get; init; }
    public required string HighlightColor { get; init; }
}
