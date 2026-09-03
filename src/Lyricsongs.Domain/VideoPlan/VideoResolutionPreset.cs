using System.Text.Json.Serialization;

namespace Lyricsongs.Domain.VideoPlan;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum VideoResolutionPreset
{
    FullHd1080p,   // 1920x1080
    Hd720p,        // 1280x720
    Vertical1080,  // 1080x1920
    Vertical720,   // 720x1280
    Square1080     // 1080x1080
}
