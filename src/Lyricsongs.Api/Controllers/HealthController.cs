using Lyricsongs.Application.Rendering;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Lyricsongs.Api.Controllers;

[ApiController]
[Route("api/v1/health")]
public sealed class HealthController : ControllerBase
{
    private readonly IFfmpegHealthCheck _ffmpeg;
    private readonly FfmpegOptions _ffmpegOptions;

    public HealthController(IFfmpegHealthCheck ffmpeg, IOptions<FfmpegOptions> ffmpegOptions)
    {
        _ffmpeg = ffmpeg;
        _ffmpegOptions = ffmpegOptions.Value;
    }

    [HttpGet]
    public async Task<ActionResult<HealthStatusDto>> GetAsync(CancellationToken cancellationToken)
    {
        var ffmpegOk = await _ffmpeg.IsAvailableAsync(cancellationToken);
        return Ok(new HealthStatusDto
        {
            Status = ffmpegOk ? "healthy" : "degraded",
            Api = "ok",
            Ffmpeg = ffmpegOk ? "available" : "missing",
            VideoEncoder = _ffmpegOptions.ResolvedVideoEncoder,
            HardwareEncoding = _ffmpegOptions.IsHardwareEncoder,
            Timestamp = DateTimeOffset.UtcNow
        });
    }
}

public sealed class HealthStatusDto
{
    public required string Status { get; init; }
    public required string Api { get; init; }
    public required string Ffmpeg { get; init; }
    public required string VideoEncoder { get; init; }
    public bool HardwareEncoding { get; init; }
    public DateTimeOffset Timestamp { get; init; }
}
