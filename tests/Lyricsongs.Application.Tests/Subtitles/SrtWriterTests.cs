using Lyricsongs.Domain.Subtitles;
using Lyricsongs.Infrastructure.Subtitles;

namespace Lyricsongs.Application.Tests.Subtitles;

public sealed class SrtWriterTests
{
    [Fact]
    public void Serialize_WritesClassicSrtBlocks()
    {
        var cues = new List<LyricCue>
        {
            new()
            {
                Index = 1,
                Start = "00:00:01.000",
                End = "00:00:04.500",
                Text = "First line",
                StartMs = 1_000,
                EndMs = 4_500,
                DurationMs = 3_500
            },
            new()
            {
                Index = 2,
                Start = "00:00:05.000",
                End = "00:00:08.250",
                Text = "Second\nline",
                StartMs = 5_000,
                EndMs = 8_250,
                DurationMs = 3_250
            }
        };

        var content = SrtWriter.Serialize(cues);

        Assert.Contains("00:00:01,000 --> 00:00:04,500", content);
        Assert.Contains("First line", content);
        Assert.Contains("00:00:05,000 --> 00:00:08,250", content);
        Assert.Contains("Second\nline", content);
        Assert.StartsWith("1", content.TrimStart());
    }
}
