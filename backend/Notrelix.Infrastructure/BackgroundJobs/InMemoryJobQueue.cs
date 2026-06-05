using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Notrelix.Application.Common.Interfaces;

namespace Notrelix.Infrastructure.BackgroundJobs;

public sealed class InMemoryJobQueue : IJobQueue
{
    private readonly ILogger<InMemoryJobQueue> _logger;
    private readonly ConcurrentQueue<QueuedJob> _jobs = new();

    public InMemoryJobQueue(ILogger<InMemoryJobQueue> logger)
    {
        _logger = logger;
    }

    public Task EnqueueAsync<TJob>(TJob job, CancellationToken cancellationToken = default)
        where TJob : class
    {
        ArgumentNullException.ThrowIfNull(job);
        cancellationToken.ThrowIfCancellationRequested();

        _jobs.Enqueue(new QueuedJob(job, DateTimeOffset.UtcNow));
        _logger.LogInformation("Queued background job {JobType}", typeof(TJob).Name);

        return Task.CompletedTask;
    }

    public Task EnqueueAsync<TJob>(TJob job, TimeSpan delay, CancellationToken cancellationToken = default)
        where TJob : class
    {
        ArgumentNullException.ThrowIfNull(job);
        cancellationToken.ThrowIfCancellationRequested();

        _jobs.Enqueue(new QueuedJob(job, DateTimeOffset.UtcNow.Add(delay)));
        _logger.LogInformation(
            "Queued delayed background job {JobType} for {ScheduledAt}",
            typeof(TJob).Name,
            DateTimeOffset.UtcNow.Add(delay));

        return Task.CompletedTask;
    }

    private sealed record QueuedJob(object Job, DateTimeOffset ScheduledAt);
}
