using System.Text.Json.Serialization;

namespace Lyricsongs.Domain.VideoPlan;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BackgroundPlanningMode
{
    /// <summary>Use SortOrder / user order as-is.</summary>
    Sequential,

    /// <summary>Deterministic shuffle using seed.</summary>
    Random,

    /// <summary>Alternate images and videos for visual variety.</summary>
    Balanced
}
