using Notrelix.Application.Common.Diagnostics;
using Notrelix.Application.Events.Automation;
using Notrelix.Application.Features.Automation.Abstractions;
using Notrelix.Application.Features.Integrations;
using Notrelix.Domain.Automation.Executions;

namespace Notrelix.Infrastructure.Messaging.Consumers.Automation;

/// <summary>
/// Performs the external n8n dispatch for a durable <see cref="N8nDispatchRequestedV1"/>.
/// At-least-once delivery; <see cref="N8nDispatchRequestedV1.ExecutionId"/> is the
/// stable idempotency/correlation identity propagated to n8n so receiving workflows
/// can deduplicate retries. Consumer dedup is enforced by the inbox/dedup filter.
/// </summary>
public sealed class N8nDispatchConsumer : IConsumer<N8nDispatchRequestedV1>
{
    private readonly IAutomationDbContext _context;
    private readonly IN8nClient _n8nClient;
    private readonly ILogger<N8nDispatchConsumer> _logger;
    private readonly PipelineMetrics _metrics;

    public N8nDispatchConsumer(
        IAutomationDbContext context,
        IN8nClient n8nClient,
        ILogger<N8nDispatchConsumer> logger,
        PipelineMetrics? metrics = null)
    {
        _context = context;
        _n8nClient = n8nClient;
        _logger = logger;
        _metrics = metrics ?? new PipelineMetrics();
    }

    public async Task Consume(ConsumeContext<N8nDispatchRequestedV1> context)
    {
        var message = context.Message;

        var execution = await _context.AutomationExecutions
            .FirstOrDefaultAsync(x => x.Id == message.ExecutionId, context.CancellationToken);

        if (execution is null)
        {
            _logger.LogWarning("Skipping n8n dispatch because execution {ExecutionId} was not found", message.ExecutionId);
            return;
        }

        // Redelivery of a previously failed attempt re-opens the SAME execution —
        // ExecutionId stays stable across retries.
        if (execution.Status == AutomationExecutionStatus.Failed)
        {
            execution.RequeueForRedelivery(message.OccurredAt);
        }

        var rule = await _context.AutomationRules
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == message.RuleId, context.CancellationToken);

        if (rule is null || !rule.IsEnabled)
        {
            FailExecution(execution, "Automation rule is missing or disabled.");
            await _context.SaveChangesAsync(context.CancellationToken);
            return;
        }

        if (!TryGetWebhookPath(rule.Configuration, out var webhookPath))
        {
            FailExecution(execution, "Automation rule is missing configuration.webhookPath.");
            await _context.SaveChangesAsync(context.CancellationToken);
            return;
        }

        // The ExecutionId is the stable external idempotency/correlation identity.
        var payload = BuildPayload(message);

        var dispatchStopwatch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var result = await _n8nClient.TriggerWebhookAsync(webhookPath, payload, context.CancellationToken);

            if (result.Succeeded)
            {
                execution.Start(message.OccurredAt);
                execution.Succeed(DateTimeOffset.UtcNow);
                execution.SetPayload(payload);
                _metrics.N8nDispatchSucceeded.Add(1);
                _metrics.N8nDispatchDuration.Record(dispatchStopwatch.Elapsed.TotalMilliseconds);
            }
            else
            {
                FailExecution(execution, $"n8n returned HTTP {result.StatusCode}: {result.Error}");
                _metrics.N8nDispatchFailed.Add(1);
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            FailExecution(execution, ex.Message);
            _metrics.N8nDispatchFailed.Add(1);
            _metrics.N8nDispatchRetries.Add(1);

            // Persist the failure state, then rethrow so the broker's retry
            // contract redelivers this durable intent (at-least-once).
            await _context.SaveChangesAsync(context.CancellationToken);
            throw;
        }

        await _context.SaveChangesAsync(context.CancellationToken);
    }

    private static string BuildPayload(N8nDispatchRequestedV1 message) =>
        System.Text.Json.JsonSerializer.Serialize(new
        {
            executionId = message.ExecutionId,
            ruleId = message.RuleId,
            accountId = message.AccountIdValue,
            workspaceId = message.WorkspaceIdValue,
            correlationId = message.CorrelationId,
        });

    private static void FailExecution(AutomationExecution execution, string error)
    {
        if (execution.Status == AutomationExecutionStatus.Queued)
        {
            execution.Start(DateTimeOffset.UtcNow);
        }

        execution.Fail(error, DateTimeOffset.UtcNow);
    }

    private static bool TryGetWebhookPath(Domain.Automation.RulesEngine.AutomationConfiguration config, out string webhookPath)
    {
        if (config.Action.Type != "Webhook" || string.IsNullOrWhiteSpace(config.Action.Configuration))
        {
            webhookPath = string.Empty;
            return false;
        }

        return N8nAutomationConfiguration.TryGetWebhookPath(config.Action.Configuration, out webhookPath);
    }
}
