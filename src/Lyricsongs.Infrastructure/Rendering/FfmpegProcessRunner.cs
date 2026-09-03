using System.Diagnostics;
using System.Text;
using Lyricsongs.Application.Rendering;
using Lyricsongs.Domain.Rendering;
using Microsoft.Extensions.Options;

namespace Lyricsongs.Infrastructure.Rendering;

public sealed class FfmpegProcessRunner : IFfmpegProcessRunner
{
    private readonly FfmpegOptions _options;
    private readonly IFfmpegProgressParser _progressParser;

    public FfmpegProcessRunner(IOptions<FfmpegOptions> options, IFfmpegProgressParser progressParser)
    {
        _options = options.Value;
        _progressParser = progressParser;
    }

    public async Task<FfmpegExecutionResult> RunAsync(
        IReadOnlyList<string> arguments,
        long? totalDurationMs = null,
        IProgress<FfmpegProgressUpdate>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var allArgs = new List<string>
        {
            "-nostdin",
            "-progress", "pipe:1",
            "-nostats"
        };
        allArgs.AddRange(arguments);

        var psi = new ProcessStartInfo
        {
            FileName = _options.ExecutablePath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var arg in allArgs)
        {
            psi.ArgumentList.Add(arg);
        }

        var stderr = new StringBuilder();
        var stdout = new StringBuilder();
        var stopwatch = Stopwatch.StartNew();

        using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is null)
            {
                return;
            }

            stdout.AppendLine(e.Data);
            var update = _progressParser.ParseProgressLine(e.Data);
            if (update is not null)
            {
                progress?.Report(update);
            }
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is null)
            {
                return;
            }

            stderr.AppendLine(e.Data);
            var update = _progressParser.ParseStatsLine(e.Data);
            if (update is not null)
            {
                progress?.Report(update);
            }
        };

        if (!process.Start())
        {
            throw new InvalidOperationException($"Failed to start FFmpeg process '{_options.ExecutablePath}'.");
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await using var registration = cancellationToken.Register(() =>
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch
            {
                // Process may have already exited.
            }
        });

        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }

            throw;
        }

        stopwatch.Stop();

        return new FfmpegExecutionResult
        {
            ExitCode = process.ExitCode,
            Arguments = allArgs,
            CommandLine = FfmpegPathHelper.ToCommandLine(_options.ExecutablePath, allArgs),
            StandardError = stderr.ToString(),
            StandardOutput = stdout.ToString(),
            Elapsed = stopwatch.Elapsed
        };
    }
}
