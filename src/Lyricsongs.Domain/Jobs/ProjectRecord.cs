namespace Lyricsongs.Domain.Jobs;

public sealed class ProjectRecord
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? AudioRelativePath { get; set; }
    public string? AudioFileName { get; set; }
    public long? AudioDurationMs { get; set; }
    public string? SrtRelativePath { get; set; }
    public string? SrtFileName { get; set; }
    public int? LyricCount { get; set; }
    public string? SettingsJson { get; set; }
    public List<ProjectAssetRecord> Assets { get; set; } = [];
}

public sealed class ProjectAssetRecord
{
    public string Id { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public string Type { get; set; } = "image";
    public long? SourceDurationMs { get; set; }
    public int SortOrder { get; set; }
}
