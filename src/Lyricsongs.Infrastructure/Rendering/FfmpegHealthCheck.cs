using System.Diagnostics;
using Lyricsongs.Application.Rendering;
using Lyricsongs.Domain.Rendering;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lyricsongs.Infrastructure.Rendering;

public sealed class FfmpegHealthCheck : IFfmpegHealthCheck
{
    private readonly FfmpegOptions _options;

    public FfmpegHealthCheck(IOptions<FfmpegOptions> options)
    {
        _options = options.Value;
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = _options.ExecutablePath,
                Arguments = "-version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process is null)
            {
                return false;
            }

            await process.WaitForExitAsync(cancellationToken);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
