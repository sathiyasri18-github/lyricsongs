using System.Text.Json.Serialization;

namespace Lyricsongs.Domain.Lyrics;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LyricPosition
{
    Top,
    BelowTop,
    Center,
    AboveBottom,
    Bottom
}
