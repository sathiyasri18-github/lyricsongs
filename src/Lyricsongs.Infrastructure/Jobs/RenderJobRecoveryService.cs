using Lyricsongs.Application.Jobs;
using Lyricsongs.Domain.Jobs;
using Microsoft.Extensions.Logging;

namespace Lyricsongs.Infrastructure.Jobs;

public sealed class RenderJobRecoveryService : IRenderJobRecovery
{
    private readonly IRenderJobStore _jobs;
    private readonly IRenderJobQueue _queue;
    private readonly ILogger<RenderJobRecoveryService> _logger;

    public RenderJobRecoveryService(
        IRenderJobStore jobs,
        IRenderJobQueue queue,
        ILogger<RenderJobRecoveryService> logger)
    {
        _jobs = jobs;
        _queue = queue;
        _logger = logger;
    }

    public async Task RecoverAsync(CancellationToken cancellationToken = default)
    {
        var jobs = await _jobs.ListAsync(cancellationToken: cancellationToken);
        foreach (var job in jobs)
        {
            if (job.Status is RenderJobStatus.Completed or RenderJobStatus.Failed or RenderJobStatus.Cancelled)
            {
                continue;
            }

            _logger.LogInformation("Recovering incomplete render job {JobId} (was {Status})", job.Id, job.Status);
            job.Status = RenderJobStatus.Queued;
            job.ProgressPercent = 0;
            job.Message = "Re-queued after restart";
            job.CancelRequested = false;
            job.StartedAt = null;
            job.ErrorMessage = null;
            await _jobs.SaveAsync(job, cancellationToken);
            await _queue.EnqueueAsync(job.Id, cancellationToken);
        }
    }
}
