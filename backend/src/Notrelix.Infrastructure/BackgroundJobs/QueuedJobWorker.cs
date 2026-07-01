using Notrelix.Application.Features.Automation.Jobs;

namespace Notrelix.Infrastructure.BackgroundJobs;

internal sealed class QueuedJobWorker : BackgroundService
{
    private readonly IBackgroundJobQueueReader _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<QueuedJobWorker> _logger;

    public QueuedJobWorker(
        IBackgroundJobQueueReader queue,
        IServiceScopeFactory scopeFactory,
        ILogger<QueuedJobWorker> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var job = await _queue.DequeueAsync(stoppingToken);
                await ProcessJobAsync(job, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background job worker failed while processing a queued job");
            }
        }
    }

    private async Task ProcessJobAsync(object job, CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();

        switch (job)
        {
            case N8nDispatchJob n8nDispatchJob:
                var dispatcher = scope.ServiceProvider.GetRequiredService<N8nDispatchService>();
                await dispatcher.DispatchAsync(n8nDispatchJob, cancellationToken);
                break;
            default:
                _logger.LogDebug("Skipping unsupported background job {JobType}", job.GetType().Name);
                break;
        }
    }
}
