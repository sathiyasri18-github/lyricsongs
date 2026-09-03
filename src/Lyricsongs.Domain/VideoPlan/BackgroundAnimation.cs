using System.Text.Json.Serialization;

namespace Lyricsongs.Domain.VideoPlan;

/// <summary>
/// Predefined background image motion. Users cannot supply arbitrary FFmpeg expressions.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BackgroundAnimation
{
    None,
    ZoomIn,
    ZoomOut,
    PanLeft,
    PanRight,
    PanUp,
    PanDown,
    SlowZoomIn,
    SlowZoomOut
}
