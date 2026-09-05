using Lyricsongs.Application.Jobs;
using Lyricsongs.Domain.Jobs;
using Lyricsongs.Domain.VideoPlan;

namespace Lyricsongs.Infrastructure.Jobs;

public sealed class RenderJobService : IRenderJobService
{
    private readonly IProjectStore _projects;
    private readonly IProjectFileStorage _storage;
    private readonly IRenderJobStore _jobs;
    private readonly IRenderJobQueue _queue;
    private readonly IRenderJobCancellationRegistry _cancellation;
    private readonly IProjectRenderValidator _validator;

    public RenderJobService(
        IProjectStore projects,
        IProjectFileStorage storage,
        IRenderJobStore jobs,
        IRenderJobQueue queue,
        IRenderJobCancellationRegistry cancellation,
        IProjectRenderValidator validator)
    {
        _projects = projects;
        _storage = storage;
        _jobs = jobs;
        _queue = queue;
        _cancellation = cancellation;
        _validator = validator;
    }

    public async Task<RenderJobDto> EnqueueAsync(
        string projectId,
        VideoPlanSettings? settings = null,
        CancellationToken cancellationToken = default)
    {
        var project = await _projects.GetAsync(projectId, cancellationToken)
            ?? throw new InvalidOperationException($"Project '{projectId}' was not found.");

        if (settings is not null)
        {
            project.SettingsJson = System.Text.Json.JsonSerializer.Serialize(settings);
            await _projects.SaveAsync(project, cancellationToken);
        }

        var validation = _validator.Validate(project, _storage);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException(string.Join(' ', validation.Errors));
        }

        var job = new RenderJobRecord
        {
            Id = Guid.NewGuid().ToString("N"),
            ProjectId = project.Id,
            ProjectName = project.Name,
            Status = RenderJobStatus.Queued,
            ProgressPercent = 0,
            Message = "Queued",
            SettingsJson = project.SettingsJson,
            OutputFileName = BuildOutputFileName(project.Name),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await _jobs.SaveAsync(job, cancellationToken);
        await _queue.EnqueueAsync(job.Id, cancellationToken);
        return ToDto(job);
    }

    public async Task<RenderJobDto?> GetAsync(string jobId, CancellationToken cancellationToken = default)
    {
        var job = await _jobs.GetAsync(jobId, cancellationToken);
        return job is null ? null : ToDto(job);
    }

    public async Task<IReadOnlyList<RenderJobDto>> ListAsync(
        string? projectId = null,
        CancellationToken cancellationToken = default)
    {
        var jobs = await _jobs.ListAsync(projectId, cancellationToken);
        return jobs.Select(ToDto).ToList();
    }

    public async Task<bool> CancelAsync(string jobId, CancellationToken cancellationToken = default)
    {
        var job = await _jobs.GetAsync(jobId, cancellationToken);
        if (job is null)
        {
            return false;
        }

        if (job.Status is RenderJobStatus.Completed or RenderJobStatus.Failed or RenderJobStatus.Cancelled)
        {
            return true;
        }

        job.CancelRequested = true;
        if (job.Status == RenderJobStatus.Queued)
        {
            job.Status = RenderJobStatus.Cancelled;
            job.Message = "Cancelled before start";
            job.CompletedAt = DateTimeOffset.UtcNow;
        }
        else
        {
            job.Message = "Cancel requested";
            _cancellation.Cancel(jobId);
        }

        await _jobs.SaveAsync(job, cancellationToken);
        return true;
    }

    private RenderJobDto ToDto(RenderJobRecord job) => new()
    {
        JobId = job.Id,
        ProjectId = job.ProjectId,
        ProjectName = job.ProjectName,
        Status = job.Status.ToString(),
        Progress = job.ProgressPercent,
        Message = job.Message,
        ErrorMessage = job.ErrorMessage,
        OutputUrl = job.Status == RenderJobStatus.Completed
            ? _storage.GetDownloadUrlPath(job.ProjectId, job.Id)
            : null,
        CreatedAt = job.CreatedAt,
        CompletedAt = job.CompletedAt
    };

    private static string BuildOutputFileName(string projectName)
    {
        var safe = string.Join("_", projectName.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        if (string.IsNullOrWhiteSpace(safe))
        {
            safe = "lyric-video";
        }

        return $"{safe}.mp4";
    }
}
