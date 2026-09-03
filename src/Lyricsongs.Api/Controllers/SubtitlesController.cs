using Lyricsongs.Application.Subtitles;
using Lyricsongs.Application.Subtitles.Models;
using Microsoft.AspNetCore.Mvc;

namespace Lyricsongs.Api.Controllers;

[ApiController]
[Route("api/v1/subtitles")]
public sealed class SubtitlesController : ControllerBase
{
    private readonly ISrtProcessingService _srtProcessingService;

    public SubtitlesController(ISrtProcessingService srtProcessingService)
    {
        _srtProcessingService = srtProcessingService;
    }

    /// <summary>
    /// Parses and validates an uploaded SRT lyrics file.
    /// Supports UTF-8 (with BOM), Tamil/English Unicode, and multiline cues.
    /// </summary>
    [HttpPost("parse")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    [ProducesResponseType(typeof(SrtProcessResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SrtProcessResponseDto>> ParseAsync(
        [FromForm] IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid upload",
                Detail = "An SRT file is required.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var extension = Path.GetExtension(file.FileName);
        if (!string.IsNullOrEmpty(extension) &&
            !extension.Equals(".srt", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid file type",
                Detail = "Only .srt files are accepted.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        await using var stream = file.OpenReadStream();
        // Copy to memory so StreamReader can seek/detect BOM safely even if upload stream is non-seekable.
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory, cancellationToken);
        memory.Position = 0;

        var result = _srtProcessingService.Process(memory);
        var response = SrtDtoMapper.ToResponse(result);

        return Ok(response);
    }

    /// <summary>
    /// Parses and validates raw SRT text from the request body.
    /// Content-Type: text/plain or application/x-subrip
    /// </summary>
    [HttpPost("parse-text")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    [ProducesResponseType(typeof(SrtProcessResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SrtProcessResponseDto>> ParseTextAsync(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var content = await reader.ReadToEndAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(content))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid body",
                Detail = "SRT text body is required.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var result = _srtProcessingService.Process(content);
        return Ok(SrtDtoMapper.ToResponse(result));
    }
}
