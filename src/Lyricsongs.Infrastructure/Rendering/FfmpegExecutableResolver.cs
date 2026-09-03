using Lyricsongs.Application.Rendering;

namespace Lyricsongs.Infrastructure.Rendering;

internal static class FfmpegExecutableResolver
{
    public static void Resolve(FfmpegOptions options)
    {
        options.ExecutablePath = ResolveExecutable(
            options.ExecutablePath,
            "ffmpeg",
            FindWingetExecutable);

        if (TryResolveSiblingFfprobe(options.ExecutablePath) is { } siblingFfprobe)
        {
            options.FFprobeExecutablePath = siblingFfprobe;
            return;
        }

        options.FFprobeExecutablePath = ResolveExecutable(
            options.FFprobeExecutablePath ?? "ffprobe",
            "ffprobe",
            FindWingetFfprobe);
    }

    private static string? TryResolveSiblingFfprobe(string ffmpegPath)
    {
        if (!IsExistingFile(ffmpegPath))
        {
            return null;
        }

        var sibling = Path.Combine(
            Path.GetDirectoryName(ffmpegPath)!,
            OperatingSystem.IsWindows() ? "ffprobe.exe" : "ffprobe");

        return File.Exists(sibling) ? sibling : null;
    }

    private static string ResolveExecutable(
        string configuredPath,
        string executableName,
        Func<string?> findKnownInstall)
    {
        if (IsExistingFile(configuredPath))
        {
            return configuredPath;
        }

        var onPath = FindOnPath(executableName);
        if (onPath is not null)
        {
            return onPath;
        }

        var knownInstall = findKnownInstall();
        if (knownInstall is not null)
        {
            return knownInstall;
        }

        return configuredPath;
    }

    private static bool IsExistingFile(string path) =>
        Path.IsPathRooted(path) && File.Exists(path);

    private static string? FindOnPath(string executableName)
    {
        var pathValue = string.Join(
            Path.PathSeparator,
            new[]
            {
                Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine),
                Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User),
                Environment.GetEnvironmentVariable("PATH")
            }.Where(static value => !string.IsNullOrWhiteSpace(value)));

        foreach (var directory in pathValue.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            var candidate = Path.Combine(directory.Trim(), executableName);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            if (OperatingSystem.IsWindows())
            {
                var withExtension = candidate.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                    ? candidate
                    : candidate + ".exe";

                if (File.Exists(withExtension))
                {
                    return withExtension;
                }
            }
        }

        return null;
    }

    private static string? FindWingetExecutable() => FindWingetBinary("ffmpeg.exe");

    private static string? FindWingetFfprobe() => FindWingetBinary("ffprobe.exe");

    private static string? FindWingetBinary(string fileName)
    {
        var packagesRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Microsoft",
            "WinGet",
            "Packages");

        if (!Directory.Exists(packagesRoot))
        {
            return null;
        }

        foreach (var packageDir in Directory.EnumerateDirectories(packagesRoot, "Gyan.FFmpeg*"))
        {
            foreach (var buildDir in Directory.EnumerateDirectories(packageDir, "ffmpeg-*"))
            {
                var candidate = Path.Combine(buildDir, "bin", fileName);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
        }

        return null;
    }
}
