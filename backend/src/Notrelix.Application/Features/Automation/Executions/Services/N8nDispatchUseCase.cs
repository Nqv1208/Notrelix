using Notrelix.Application.Features.Automation.Abstractions;
using Notrelix.Application.Features.Integrations.N8n.Configuration;
using Notrelix.Application.Features.Integrations.Public.Commands;

namespace Notrelix.Application.Features.Automation.Executions.Services;

/// <summary>
/// Automation-owned dispatch use case for a durable
/// <see cref="Application.Events.Automation.N8nDispatchRequestedV1"/> intent.
/// Owns the full AutomationExecution lifecycle progression: start the attempt,
/// call the Integrations-owned webhook semantic action, classify the outcome,
/// and persist succeed / terminal fail / retryable-failure state.
/// </summary>
public sealed class N8nDispatchUseCase
{
    private readonly IAutomationDbContext _context;
    private readonly IN8nWebhookActions _n8nWebhookActions;
    private readonly IDateTimeProvider _clock;

    public N8nDispatchUseCase(
        IAutomationDbContext context,
        IN8nWebhookActions n8nWebhookActions,
        IDateTimeProvider clock)
    {
        _context = context;
        _n8nWebhookActions = n8nWebhookActions;
        _clock = clock;
    }

    /// <summary>
    /// Executes the dispatch for one durable execution intent. Never throws for
    /// business outcomes. Returns true when the intent is complete (succeeded,
    /// terminal failure, or nothing to progress) and false when the delivery
    /// mechanism should redeliver the durable intent (retryable/unknown
    /// provider outcome) so the Automation attempt can run again.
    /// </summary>
    public async Task<bool> ExecuteAsync(
        Application.Events.Automation.N8nDispatchRequestedV1 message,
        CancellationToken cancellationToken)
    {
        var execution = await _context.AutomationExecutions
            .FirstOrDefaultAsync(x => x.Id == message.ExecutionId, cancellationToken);

        if (execution is null)
            return true; // nothing to progress; caller logs and completes

        if (execution.Status == AutomationExecutionStatus.Queued)
            execution.Start(_clock.UtcNow);

        var rule = await _context.AutomationRules
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == message.RuleId, cancellationToken);

        if (rule is null || !rule.IsEnabled)
        {
            execution.Fail("Automation rule is missing or disabled.", _clock.UtcNow);
            return true;
        }

        if (string.IsNullOrWhiteSpace(rule.Configuration.Action.Configuration) ||
            !N8nAutomationConfiguration.TryGetWebhookPath(rule.Configuration.Action.Configuration, out var webhookPath))
        {
            execution.Fail("Automation rule is missing configuration.webhookPath.", _clock.UtcNow);
            return true;
        }

        var now = _clock.UtcNow;
        var result = await _n8nWebhookActions.TriggerWebhookAsync(
            webhookPath,
            BuildPayload(message),
            cancellationToken);

        switch (result.Outcome)
        {
            case N8nWebhookOutcome.Succeeded:
                execution.SetPayload(BuildPayload(message));
                execution.Succeed(now);
                return true;

            case N8nWebhookOutcome.TerminalFailure:
                execution.Fail(result.Error ?? "n8n webhook rejected the dispatch.", now);
                return true;

            case N8nWebhookOutcome.RetryableFailure:
                // Automation-owned retry: evidence recorded, execution re-queued
                // so a later Automation attempt runs under the same identity.
                execution.RecordRetryableDispatchFailure(result.Error ?? "retryable dispatch failure", now);
                await _context.SaveChangesAsync(cancellationToken);
                return false;

            case N8nWebhookOutcome.UnknownOutcome:
            default:
                // Unknown outcome must not guess the execution's fate; leave the
                // attempt running and surface a technical retry to the caller.
                await _context.SaveChangesAsync(cancellationToken);
                return false;
        }
    }

    private static string BuildPayload(Application.Events.Automation.N8nDispatchRequestedV1 message) =>
        System.Text.Json.JsonSerializer.Serialize(new
        {
            executionId = message.ExecutionId,
            ruleId = message.RuleId,
            accountId = message.AccountIdValue,
            workspaceId = message.WorkspaceIdValue,
            correlationId = message.CorrelationId,
        });
}
