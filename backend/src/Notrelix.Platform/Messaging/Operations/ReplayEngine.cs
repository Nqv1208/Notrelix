using Microsoft.Extensions.Logging;
using Notrelix.Platform.Messaging.Runtime;

namespace Notrelix.Platform.Messaging.Operations;

public sealed class ReplayEngine
{
    private readonly IMessagingRuntime _runtime;
    private readonly IReplayCheckpointStore _checkpointStore;
    private readonly IReplayAuditLog _auditLog;
    private readonly ILogger<ReplayEngine>? _logger;

    private long _totalPublished;
    private bool _cancelled;

    public ReplayEngine(
        IMessagingRuntime runtime,
        IReplayCheckpointStore checkpointStore,
        IReplayAuditLog auditLog,
        ILogger<ReplayEngine>? logger = null)
    {
        _runtime = runtime;
        _checkpointStore = checkpointStore;
        _auditLog = auditLog;
        _logger = logger;
    }

    public async Task<ReplayResult> ExecuteAsync(
        ReplayRequest request,
        IReplayStrategy strategy,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(strategy);

        _totalPublished = 0;
        _cancelled = false;

        var auditId = await _auditLog.StartAsync(request, cancellationToken);
        var sw = ValueStopwatch.StartNew();
        var totalRequested = 0L;
        var totalFailed = 0L;
        long? checkpointId = null;

        try
        {
            var throttle = new ReplayThrottle(request.MaxEventsPerSecond);

            await foreach (var publication in strategy.GetEventsAsync(request, _checkpointStore, cancellationToken))
            {
                if (cancellationToken.IsCancellationRequested || _cancelled)
                {
                    _logger?.LogInformation("Replay cancelled after {Count} events", _totalPublished);
                    var cancelledResult = ReplayResult.Cancelled(_totalPublished, sw.Elapsed);
                    await _auditLog.UpdateAsync(auditId, cancelledResult, cancellationToken);
                    return cancelledResult;
                }

                totalRequested++;

                await throttle.WaitAsync(cancellationToken);

                try
                {
                    var publishResult = await _runtime.PublishAsync(publication, cancellationToken);

                    if (publishResult.Success)
                    {
                        _totalPublished++;
                        var checkpoint = await _checkpointStore.SaveAsync(
                            request.EventName, request.WorkspaceId, _totalPublished, cancellationToken);
                        checkpointId = checkpoint.Id;
                    }
                    else
                    {
                        totalFailed++;
                        _logger?.LogWarning("Replay publish failed for event {EventName}: {Errors}",
                            request.EventName, string.Join("; ", publishResult.Errors ?? []));
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    totalFailed++;
                    _logger?.LogError(ex, "Replay publish threw for event {EventName}", request.EventName);
                }
            }

            var result = ReplayResult.Completed(totalRequested, _totalPublished, totalFailed, sw.Elapsed, checkpointId);
            await _auditLog.UpdateAsync(auditId, result, cancellationToken);

            _logger?.LogInformation(
                "Replay completed: {Published}/{Requested} published, {Failed} failed in {Duration}",
                _totalPublished, totalRequested, totalFailed, sw.Elapsed);

            return result;
        }
        catch (OperationCanceledException)
        {
            var cancelledResult = ReplayResult.Cancelled(_totalPublished, sw.Elapsed);
            await _auditLog.UpdateAsync(auditId, cancelledResult, cancellationToken);
            return cancelledResult;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Replay failed");
            var failedResult = ReplayResult.Failed(ex.Message);
            await _auditLog.UpdateAsync(auditId, failedResult, cancellationToken);
            return failedResult;
        }
    }

    public void Cancel()
    {
        _cancelled = true;
    }
}
