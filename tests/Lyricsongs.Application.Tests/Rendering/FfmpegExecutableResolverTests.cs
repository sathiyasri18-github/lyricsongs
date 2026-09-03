using Lyricsongs.Application.Rendering;
using Lyricsongs.Infrastructure.Rendering;

namespace Lyricsongs.Application.Tests.Rendering;

public class FfmpegExecutableResolverTests
{
    [Fact]
    public void Resolve_KeepsExistingAbsolutePath()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            var ffmpegPath = Path.Combine(tempDir, "ffmpeg.exe");
            File.WriteAllText(ffmpegPath, string.Empty);

            var options = new FfmpegOptions
            {
                ExecutablePath = ffmpegPath,
                FFprobeExecutablePath = Path.Combine(tempDir, "ffprobe.exe")
            };
            File.WriteAllText(options.FFprobeExecutablePath, string.Empty);

            FfmpegExecutableResolver.Resolve(options);

            Assert.Equal(ffmpegPath, options.ExecutablePath);
            Assert.Equal(Path.Combine(tempDir, "ffprobe.exe"), options.FFprobeExecutablePath);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void Resolve_DerivesFfprobeFromResolvedFfmpegDirectory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            var ffmpegPath = Path.Combine(tempDir, "ffmpeg.exe");
            var ffprobePath = Path.Combine(tempDir, "ffprobe.exe");
            File.WriteAllText(ffmpegPath, string.Empty);
            File.WriteAllText(ffprobePath, string.Empty);

            var options = new FfmpegOptions
            {
                ExecutablePath = ffmpegPath,
                FFprobeExecutablePath = "ffprobe"
            };

            FfmpegExecutableResolver.Resolve(options);

            Assert.Equal(ffprobePath, options.FFprobeExecutablePath);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }
}
