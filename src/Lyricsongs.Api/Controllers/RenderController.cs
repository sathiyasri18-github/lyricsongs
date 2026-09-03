using Lyricsongs.Application.Jobs;
using Microsoft.AspNetCore.Mvc;

namespace Lyricsongs.Api.Controllers;

[ApiController]
[Route("api/v1/render")]
public sealed class RenderController : ControllerBase
{
    private readonly IRenderJobService _jobs;
    private readonly IRenderJobStore _jobStore;
    private readonly IProjectFileStorage _storage;

    public RenderController(
        IRenderJobService jobs,
        IRenderJobStore jobStore,
        IProjectFileStorage storage)
    {
        _jobs = jobs;
        _jobStore = jobStore;
        _storage = storage;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RenderJobDto>>> ListAsync(
        [FromQuery] string? projectId,
        CancellationToken cancellationToken)
    {
        var jobs = await _jobs.ListAsync(projectId, cancellationToken);
        return Ok(jobs);
    }

    [HttpGet("{jobId}")]
    public async Task<ActionResult<RenderJobDto>> GetAsync(string jobId, CancellationToken cancellationToken)
    {
        var job = await _jobs.GetAsync(jobId, cancellationToken);
        return job is null ? NotFound() : Ok(job);
    }

    [HttpPost("{jobId}/cancel")]
    public async Task<IActionResult> CancelAsync(string jobId, CancellationToken cancellationToken)
    {
        var cancelled = await _jobs.CancelAsync(jobId, cancellationToken);
        return cancelled ? Ok() : NotFound();
    }

    [HttpGet("{jobId}/download")]
    public async Task<IActionResult> DownloadAsync(string jobId, CancellationToken cancellationToken)
    {
        var job = await _jobStore.GetAsync(jobId, cancellationToken);
        if (job is null)
        {
            return NotFound();
        }

        var path = job.OutputFilePath;
        if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path))
        {
            // Fall back to convention path
            path = _storage.GetOutputPath(job.ProjectId, job.Id);
        }

        if (!System.IO.File.Exists(path))
        {
            return NotFound(new ProblemDetails
            {
                Title = "Output not ready",
                Detail = "The rendered MP4 is not available yet.",
                Status = StatusCodes.Status404NotFound
            });
        }

        var fileName = job.OutputFileName ?? $"{job.ProjectName}.mp4";
        return PhysicalFile(path, "video/mp4", fileName);
    }
}
