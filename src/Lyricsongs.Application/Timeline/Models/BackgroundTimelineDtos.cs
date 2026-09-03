using Lyricsongs.Domain.Timeline;
using Lyricsongs.Domain.Subtitles;

namespace Lyricsongs.Application.Timeline.Models;

public sealed class BackgroundAssetDto
{
    public string Id { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public long? SourceDurationMs { get; init; }
    public int SortOrder { get; init; }
}

public sealed class BackgroundTimelineRequestDto
{
    public long AudioDurationMs { get; init; }
    public string? AudioDurationSeconds { get; init; }
    public IReadOnlyList<BackgroundAssetDto> Assets { get; init; } = [];
    public string OrderingMode { get; init; } = "Manual";
    public int OrderingSeed { get; init; } = 42;
    public long TransitionDurationMs { get; init; } = 500;
    public long MinSegmentDurationMs { get; init; } = 3_000;
}

public sealed class TimelineSegmentDto
{
    public int Sequence { get; init; }
    public string AssetId { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public string AssetType { get; init; } = string.Empty;
    public string Start { get; init; } = string.Empty;
    public string End { get; init; } = string.Empty;
    public long StartMs { get; init; }
    public long EndMs { get; init; }
    public long SlotDurationMs { get; init; }
    public long TransitionOutDurationMs { get; init; }
    public string PlaybackMode { get; init; } = "Normal";
    public int LoopCount { get; init; } = 1;
    public long TrimStartMs { get; init; }
    public long? SourcePlayDurationMs { get; init; }
    public long? SourceDurationMs { get; init; }
}

public sealed class BackgroundTimelineSummaryDto
{
    public int SegmentCount { get; init; }
    public int ImageCount { get; init; }
    public int VideoCount { get; init; }
    public long TotalSlotDurationMs { get; init; }
    public long TotalTransitionOverlapMs { get; init; }
    public long TimelineEndMs { get; init; }
    public long AvailableVideoDurationMs { get; init; }
}

public sealed class TimelineValidationIssueDto
{
    public string Code { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public int? SegmentSequence { get; init; }
    public string? AssetId { get; init; }
}

public sealed class BackgroundTimelineResponseDto
{
    public bool IsValid { get; init; }
    public bool HasWarnings { get; init; }
    public long AudioDurationMs { get; init; }
    public string AudioDuration { get; init; } = string.Empty;
    public long AvailableVideoDurationMs { get; init; }
    public long TransitionDurationMs { get; init; }
    public string OrderingMode { get; init; } = string.Empty;
    public int OrderingSeed { get; init; }
    public IReadOnlyList<string> OrderedAssetIds { get; init; } = [];
    public IReadOnlyList<TimelineSegmentDto> Segments { get; init; } = [];
    public BackgroundTimelineSummaryDto Summary { get; init; } = new();
    public IReadOnlyList<TimelineValidationIssueDto> Errors { get; init; } = [];
    public IReadOnlyList<TimelineValidationIssueDto> Warnings { get; init; } = [];
}

public static class BackgroundTimelineDtoMapper
{
    public static BackgroundTimelineRequest ToRequest(BackgroundTimelineRequestDto dto)
    {
        return new BackgroundTimelineRequest
        {
            AudioDurationMs = dto.AudioDurationMs > 0
                ? dto.AudioDurationMs
                : ParseSecondsToMs(dto.AudioDurationSeconds),
            Assets = dto.Assets.Select(a => new BackgroundAsset
            {
                Id = a.Id,
                FileName = a.FileName,
                Type = Enum.Parse<BackgroundAssetType>(a.Type, ignoreCase: true),
                SourceDurationMs = a.SourceDurationMs,
                SortOrder = a.SortOrder
            }).ToList(),
            OrderingMode = Enum.Parse<AssetOrderingMode>(dto.OrderingMode, ignoreCase: true),
            OrderingSeed = dto.OrderingSeed,
            TransitionDurationMs = dto.TransitionDurationMs,
            MinSegmentDurationMs = dto.MinSegmentDurationMs
        };
    }

    public static BackgroundTimelineResponseDto ToResponse(BackgroundTimelineBuildResult result)
    {
        var timeline = result.Timeline;
        var validation = result.Validation;

        return new BackgroundTimelineResponseDto
        {
            IsValid = validation.IsValid,
            HasWarnings = validation.HasWarnings,
            AudioDurationMs = timeline.AudioDurationMs,
            AudioDuration = timeline.AudioDuration,
            AvailableVideoDurationMs = timeline.AvailableVideoDurationMs,
            TransitionDurationMs = timeline.TransitionDurationMs,
            OrderingMode = timeline.OrderingMode.ToString(),
            OrderingSeed = timeline.OrderingSeed,
            OrderedAssetIds = timeline.OrderedAssetIds.ToList(),
            Segments = timeline.Segments.Select(ToSegmentDto).ToList(),
            Summary = new BackgroundTimelineSummaryDto
            {
                SegmentCount = timeline.Summary.SegmentCount,
                ImageCount = timeline.Summary.ImageCount,
                VideoCount = timeline.Summary.VideoCount,
                TotalSlotDurationMs = timeline.Summary.TotalSlotDurationMs,
                TotalTransitionOverlapMs = timeline.Summary.TotalTransitionOverlapMs,
                TimelineEndMs = timeline.Summary.TimelineEndMs,
                AvailableVideoDurationMs = timeline.AvailableVideoDurationMs
            },
            Errors = validation.Issues
                .Where(i => i.Severity == TimelineValidationSeverity.Error)
                .Select(ToIssueDto)
                .ToList(),
            Warnings = validation.Issues
                .Where(i => i.Severity == TimelineValidationSeverity.Warning)
                .Select(ToIssueDto)
                .ToList()
        };
    }

    private static TimelineSegmentDto ToSegmentDto(TimelineSegment s) => new()
    {
        Sequence = s.Sequence,
        AssetId = s.AssetId,
        FileName = s.FileName,
        AssetType = s.AssetType.ToString(),
        Start = s.Start,
        End = s.End,
        StartMs = s.StartMs,
        EndMs = s.EndMs,
        SlotDurationMs = s.SlotDurationMs,
        TransitionOutDurationMs = s.TransitionOutDurationMs,
        PlaybackMode = s.PlaybackMode.ToString(),
        LoopCount = s.LoopCount,
        TrimStartMs = s.TrimStartMs,
        SourcePlayDurationMs = s.SourcePlayDurationMs,
        SourceDurationMs = s.SourceDurationMs
    };

    private static TimelineValidationIssueDto ToIssueDto(TimelineValidationIssue i) => new()
    {
        Code = i.Code,
        Message = i.Message,
        Severity = i.Severity.ToString(),
        SegmentSequence = i.SegmentSequence,
        AssetId = i.AssetId
    };

    private static long ParseSecondsToMs(string? seconds)
    {
        if (string.IsNullOrWhiteSpace(seconds))
        {
            return 0;
        }

        return (long)(double.Parse(seconds, System.Globalization.CultureInfo.InvariantCulture) * 1000);
    }
}
