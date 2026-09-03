using System.Text.Json.Serialization;

namespace Lyricsongs.Domain.Lyrics;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LyricTextAlignment
{
    Left,
    Center,
    Right
}
