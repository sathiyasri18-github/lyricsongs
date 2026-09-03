using Lyricsongs.Infrastructure.Rendering;

namespace Lyricsongs.Application.Tests.Rendering;

public class FfmpegVideoEncodeArgsTests
{
    [Fact]
    public void Build_Libx264_UsesPresetAndCrf()
    {
        var args = FfmpegVideoEncodeArgs.Build("libx264", "medium", 18).ToList();

        Assert.Equal(["-c:v", "libx264", "-preset", "medium", "-crf", "18", "-pix_fmt", "yuv420p"], args);
    }

    [Fact]
    public void Build_Nvenc_UsesConstQp()
    {
        var args = FfmpegVideoEncodeArgs.Build("h264_nvenc", "medium", 19).ToList();

        Assert.Contains("-cq", args);
        Assert.Contains("19", args);
        Assert.Contains("p4", args);
        Assert.DoesNotContain("-crf", args);
    }

    [Fact]
    public void Build_MediaFoundation_UsesQualityMode()
    {
        var args = FfmpegVideoEncodeArgs.Build("h264_mf", "medium", 18).ToList();

        Assert.Contains("h264_mf", args);
        Assert.Contains("-rate_control", args);
        Assert.Contains("quality", args);
        Assert.Contains("nv12", args);
    }
}
