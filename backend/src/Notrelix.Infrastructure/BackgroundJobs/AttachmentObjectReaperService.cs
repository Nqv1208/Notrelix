using Notrelix.Infrastructure.Storage;
namespace Notrelix.Infrastructure.BackgroundJobs;

/// <summary>
/// Hosted loop of <see cref="AttachmentObjectReaper"/>. Each pass runs in its own DI scope. Several
/// instances may run concurrently: the store claims rows with SKIP LOCKED and elects one orphan
/// sweeper through an advisory lock, so no object is processed twice at the same time.
/// </summary>
internal sealed class AttachmentObjectReaperService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AttachmentRetentionOptions _options;
    private readonly ILogger<AttachmentObjectReaperService> _logger;

    public AttachmentObjectReaperService(
        IServiceScopeFactory scopeFactory,
        IOptions<AttachmentRetentionOptions> options,
        ILogger<AttachmentObjectReaperService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("AttachmentObjectReaper is disabled");
            return;
        }

        _logger.LogInformation("AttachmentObjectReaper started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var reaper = scope.ServiceProvider.GetRequiredService<AttachmentObjectReaper>();
                await reaper.RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AttachmentObjectReaper pass failed");
            }

            try
            {
                await Task.Delay(_options.Interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }
}
