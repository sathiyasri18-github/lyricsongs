using System.Text.Json;
using System.Text.Json.Serialization;
using Lyricsongs.Domain.Lyrics;

namespace Lyricsongs.Application.Lyrics;

/// <summary>
/// Loads and validates lyric style configuration from JSON.
/// </summary>
public interface ILyricStyleConfigurationLoader
{
    LyricStyleConfiguration Load(string json);

    LyricStyleConfiguration Load(Stream stream);
}

public sealed class LyricStyleConfigurationLoader : ILyricStyleConfigurationLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public LyricStyleConfiguration Load(string json) =>
        JsonSerializer.Deserialize<LyricStyleConfiguration>(json, Options)
        ?? throw new InvalidOperationException("Lyric style configuration JSON is empty.");

    public LyricStyleConfiguration Load(Stream stream) =>
        JsonSerializer.Deserialize<LyricStyleConfiguration>(stream, Options)
        ?? throw new InvalidOperationException("Lyric style configuration JSON is empty.");
}
