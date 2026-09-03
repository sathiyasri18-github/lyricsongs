using Lyricsongs.Application.Rendering;
using Lyricsongs.Domain.Rendering;
using Lyricsongs.Domain.Timeline;
using Lyricsongs.Infrastructure.Rendering;

namespace Lyricsongs.Application.Tests.Rendering;

public class FfmpegCommandBuilderTests
{
    private readonly FfmpegCommandBuilder _builder = new(new FfmpegFilterGraphBuilder());
    private static readonly RenderOutputConfiguration Output = new();

    [Fact]
    public void BuildSegmentImageCommand_IncludesKenBurnsAndDuration()
    {
        var segment = new SegmentRenderPlan
        {
            Sequence = 1,
            SourceAssetPath = @"C:\assets\bg1.jpg",
            OutputSegmentPath = @"C:\temp\seg_001.mp4",
            DurationSeconds = 35,
            IsImage = true,
            KenBurnsMotion = KenBurnsMotion.ZoomIn
        };

        var args = _builder.BuildSegmentImageCommand(segment, Output);

        Assert.Contains("-loop", args);
        Assert.Contains("1", args);
        Assert.Contains("-t", args);
        Assert.Contains("35.000", args);
        Assert.Contains("zoompan", string.Join(' ', args));
        Assert.Contains("-c:v", args);
        Assert.Contains("libx264", args);
        Assert.Contains("-crf", args);
        Assert.Contains("-an", args);
    }

    [Fact]
    public void BuildSegmentVideoCommand_SupportsLoopAndTrim()
    {
        var segment = new SegmentRenderPlan
        {
            Sequence = 2,
            SourceAssetPath = @"C:\assets\clip1.mp4",
            OutputSegmentPath = @"C:\temp\seg_002.mp4",
            DurationSeconds = 40,
            IsImage = false,
            LoopCount = 3,
            TrimStartSeconds = 1.5
        };

        var args = _builder.BuildSegmentVideoCommand(segment, Output);

        Assert.Contains("-stream_loop", args);
        Assert.Contains("2", args);
        Assert.Contains("-ss", args);
        Assert.Contains("1.500", args);
        Assert.Contains("scale=1920:1080", string.Join(' ', args));
    }

    [Fact]
    public void BuildFinalMuxCommand_IncludesAssFilterAndAac()
    {
        var args = _builder.BuildFinalMuxCommand(
            @"C:\temp\background.mp4",
            @"C:\temp\lyrics.ass",
            @"C:\temp\song.mp3",
            @"C:\temp\output.mp4",
            Output);

        var joined = string.Join(' ', args);
        Assert.Contains("ass=", joined);
        Assert.Contains("lyrics.ass", joined);
        Assert.Contains("-c:a", args);
        Assert.Contains("aac", args);
        Assert.Contains("-shortest", args);
        Assert.Contains("+faststart", joined);
    }

    [Fact]
    public void BuildConcatDemuxerCommand_UsesConcatFormat()
    {
        var args = _builder.BuildConcatDemuxerCommand(@"C:\temp\concat.txt", @"C:\temp\background.mp4");

        Assert.Contains("-f", args);
        Assert.Contains("concat", args);
        Assert.Contains("-safe", args);
        Assert.Contains("0", args);
    }
}
