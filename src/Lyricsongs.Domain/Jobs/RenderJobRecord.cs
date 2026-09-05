using System.Text.Json.Serialization;

namespace Lyricsongs.Domain.Jobs;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RenderJobStatus
{
    Queued,
    Preparing,
    Rendering,
    Completed,
    Failed,
    Cancelled
}

public sealed class RenderJobRecord
{
    public string Id { get; set; } = string.Empty;
    public string ProjectId { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public RenderJobStatus Status { get; set; } = RenderJobStatus.Queued;
    public double ProgressPercent { get; set; }
    public string? Message { get; set; }
    public string? ErrorMessage { get; set; }
    public bool CancelRequested { get; set; }
    public string? OutputFilePath { get; set; }
    public string? OutputFileName { get; set; }
    public string? SettingsJson { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
}
