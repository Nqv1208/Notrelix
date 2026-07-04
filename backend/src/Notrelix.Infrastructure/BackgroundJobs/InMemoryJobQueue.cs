using System.Threading.Channels;

namespace Notrelix.Infrastructure.BackgroundJobs;

internal interface IBackgroundJobQueueReader
{
    ValueTask<object> DequeueAsync(CancellationToken cancellationToken = default);
}

public sealed class InMemoryJobQueue : IJobQueue, IBackgroundJobQueueReader
{
    private readonly ILogger<InMemoryJobQueue> _logger;
    private readonly Channel<QueuedJob> _jobs = Channel.CreateUnbounded<QueuedJob>();

    public InMemoryJobQueue(ILogger<InMemoryJobQueue> logger)
    {
        _logger = logger;
    }

    public Task EnqueueAsync<TJob>(TJob job, CancellationToken cancellationToken = default)
        where TJob : class
    {
        ArgumentNullException.ThrowIfNull(job);
        cancellationToken.ThrowIfCancellationRequested();

        _jobs.Writer.TryWrite(new QueuedJob(job, DateTimeOffset.UtcNow));
        _logger.LogInformation("Queued background job {JobType}", typeof(TJob).Name);

        return Task.CompletedTask;
    }

    public Task EnqueueAsync<TJob>(TJob job, TimeSpan delay, CancellationToken cancellationToken = default)
        where TJob : class
    {
        ArgumentNullException.ThrowIfNull(job);
        cancellationToken.ThrowIfCancellationRequested();

        _jobs.Writer.TryWrite(new QueuedJob(job, DateTimeOffset.UtcNow.Add(delay)));
        _logger.LogInformation(
            "Queued delayed background job {JobType} for {ScheduledAt}",
            typeof(TJob).Name,
            DateTimeOffset.UtcNow.Add(delay));

        return Task.CompletedTask;
    }

    public async ValueTask<object> DequeueAsync(CancellationToken cancellationToken = default)
    {
        var queuedJob = await _jobs.Reader.ReadAsync(cancellationToken);
        var delay = queuedJob.ScheduledAt - DateTimeOffset.UtcNow;

        if (delay > TimeSpan.Zero)
        {
            await Task.Delay(delay, cancellationToken);
        }

        return queuedJob.Job;
    }

    private sealed record QueuedJob(object Job, DateTimeOffset ScheduledAt);
}
