using System.Text.Json.Serialization;

namespace Lyricsongs.Domain.VideoPlan;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum VideoAspectRatio
{
    Landscape16x9,
    Portrait9x16,
    Square1x1
}
