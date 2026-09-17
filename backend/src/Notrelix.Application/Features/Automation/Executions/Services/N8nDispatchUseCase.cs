using Notrelix.Application.Features.Automation.Abstractions;
using Notrelix.Application.Features.Integrations.N8n.Configuration;
using Notrelix.Application.Features.Integrations.Public.Commands;

namespace Notrelix.Application.Features.Automation.Executions.Services;

/// <summary>
/// Outcome of the prepare phase: whether the consumer should make the provider
/// call, or whether nothing could/should progress and the attempt is already
/// settled inside the first transaction.
/// </summary>
public enum N8nDispatchPrepareOutcome
{
    /// <summary>The execution is missing or already terminal — nothing to progress.</summary>
    NothingToProgress,
    /// <summary>The attempt is Running and the provider call is the next step.</summary>
    ReadyForDispatch,
    /// <summary>A precondition failed (rule missing/disabled/invalid config); settled as terminal failure.</summary>
    PrerequisiteSettled,
}

/// <summary>
/// Result of the prepare phase. When <see cref="Outcome"/> is
/// <see cref="N8nDispatchPrepareOutcome.ReadyForDispatch"/>, the webhook path
/// and payload let the consumer run the provider effect OUTSIDE any database
/// transaction between the two durability transactions.
/// </summary>
public sealed record N8nDispatchPreparation(
    N8nDispatchPrepareOutcome Outcome,
    string? WebhookPath = null,
    string? Payload = null);

/// <summary>
/// Outcome of reconciling a residual <c>Processing</c> claim after a crash.
/// </summary>
public enum N8nDispatchResidueOutcome
{
    /// <summary>Nothing to do (e.g. execution already terminal).</summary>
    NothingToDo,
    /// <summary>The interrupted attempt was settled as a terminal failure requiring reconciliation.</summary>
    SettledAsFailed,
    /// <summary>The execution is Queued with a stale Processing claim — an operator sweep is required.</summary>
    RequiresOperatorSweep,
}

/// <summary>
/// Automation-owned dispatch use case for a durable
/// <see cref="Application.Events.Automation.N8nDispatchRequestedV1"/> intent,
/// split around the consumer-owned prepare/effect/settle protocol:
///
/// <list type="bullet">
///   <item><see cref="PrepareAttemptAsync"/> — inside the consumer's first
///   transaction: progress Queued → Running, validate the rule, and either hand
///   back a ready webhook call or settle early.</item>
///   <item><see cref="CallWebhookAsync"/> — the provider effect, invoked with no
///   database transaction held.</item>
///   <item><see cref="SettleAttemptAsync"/> — inside the consumer's second
///   transaction: classify the outcome into succeed / terminal fail /
///   retryable evidence (re-queued + attempt count).</item>
///   <item><see cref="SettleResidueAsync"/> — reconcile a residual
///   <c>Processing</c> claim after a crash using only the durable execution
///   state; never re-fires the provider.</item>
/// </list>
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
    /// Prepares one durable dispatch attempt inside the caller's first
    /// transaction. Progression Queued → Running is applied in-memory when a
    /// Queued execution is ready; the caller persists it. Running is treated as
    /// an interrupted residue and settled as Failed — the provider call is
    /// never re-issued from here.
    /// </summary>
    public async Task<N8nDispatchPreparation> PrepareAttemptAsync(
        Application.Events.Automation.N8nDispatchRequestedV1 message,
        CancellationToken cancellationToken)
    {
        var execution = await _context.AutomationExecutions
            .FirstOrDefaultAsync(x => x.Id == message.ExecutionId, cancellationToken);

        if (execution is null)
            return new N8nDispatchPreparation(N8nDispatchPrepareOutcome.NothingToProgress);

        if (execution.Status is AutomationExecutionStatus.Succeeded
            or AutomationExecutionStatus.Failed
            or AutomationExecutionStatus.Cancelled)
            return new N8nDispatchPreparation(N8nDispatchPrepareOutcome.NothingToProgress);

        if (execution.Status == AutomationExecutionStatus.Running)
        {
            // Interrupted attempt whose provider outcome is unknown. Unlike a
            // fresh Queued execution this must NEVER auto re-fire — settling it
            // as a terminal failure is the reconciliation-safe outcome. (The
            // durable rule: a Running execution is only re-queued by retryable
            // evidence in SettleAttemptAsync, never by redelivery alone.)
            execution.Fail(
                "Dispatch attempt interrupted — provider outcome unknown — reconciliation required",
                _clock.UtcNow);
            return new N8nDispatchPreparation(N8nDispatchPrepareOutcome.NothingToProgress);
        }

        execution.Start(_clock.UtcNow);

        var rule = await _context.AutomationRules
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == message.RuleId, cancellationToken);

        if (rule is null || !rule.IsEnabled)
        {
            execution.Fail("Automation rule is missing or disabled.", _clock.UtcNow);
            return new N8nDispatchPreparation(N8nDispatchPrepareOutcome.PrerequisiteSettled);
        }

        if (string.IsNullOrWhiteSpace(rule.Configuration.Action.Configuration) ||
            !N8nAutomationConfiguration.TryGetWebhookPath(rule.Configuration.Action.Configuration, out var webhookPath))
        {
            execution.Fail("Automation rule is missing configuration.webhookPath.", _clock.UtcNow);
            return new N8nDispatchPreparation(N8nDispatchPrepareOutcome.PrerequisiteSettled);
        }

        return new N8nDispatchPreparation(
            N8nDispatchPrepareOutcome.ReadyForDispatch,
            webhookPath,
            BuildPayload(message));
    }

    /// <summary>
    /// Runs the provider effect. Called by the consumer WHILE NO database
    /// transaction is open, satisfying the invariant that an external side
    /// effect never executes inside a transaction.
    /// </summary>
    public Task<N8nWebhookDispatchResult> CallWebhookAsync(
        string webhookPath,
        string payload,
        CancellationToken cancellationToken)
        => _n8nWebhookActions.TriggerWebhookAsync(webhookPath, payload, cancellationToken);

    /// <summary>
    /// Settles one attempt outcome inside the caller's second transaction.
    /// Returns true when the intent is terminally settled (succeeded, terminal
    /// failure, unknown-outcome reconciliation) and false when the delivery
    /// mechanism should redeliver (retryable evidence recorded). Evidence is
    /// persisted by the caller's SaveChanges — this method never flushes.
    /// </summary>
    public async Task<bool> SettleAttemptAsync(
        Application.Events.Automation.N8nDispatchRequestedV1 message,
        N8nWebhookDispatchResult result,
        CancellationToken cancellationToken)
    {
        var execution = await _context.AutomationExecutions
            .FirstOrDefaultAsync(x => x.Id == message.ExecutionId, cancellationToken);

        if (execution is null)
            return true; // nothing to progress; caller completes

        var now = _clock.UtcNow;
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
                return false;

            case N8nWebhookOutcome.UnknownOutcome:
            default:
                // Unknown outcome: the provider may or may not have processed
                // the call. MUST NOT auto re-fire — that risks duplicate
                // side-effects. Settle the execution as failed with an explicit
                // reconciliation-required signal so a human or reconciliation
                // process can resolve it.
                execution.Fail(
                    $"{result.Error ?? "n8n outcome unknown"} — reconciliation required",
                    now);
                return true;
        }
    }

    /// <summary>
    /// Reconciles a residual <c>Processing</c> claim using only the durable
    /// execution state. Guarantees no provider re-fire: a Running execution
    /// (crash between the two durable transactions) is settled as a terminal
    /// failure with a reconciliation marker, a terminal execution needs no
    /// change, and a Queued execution with a stale claim is left for an
    /// operator sweep instead of being re-run blindly.
    /// </summary>
    public async Task<N8nDispatchResidueOutcome> SettleResidueAsync(
        Application.Events.Automation.N8nDispatchRequestedV1 message,
        CancellationToken cancellationToken)
    {
        var execution = await _context.AutomationExecutions
            .FirstOrDefaultAsync(x => x.Id == message.ExecutionId, cancellationToken);

        if (execution is null)
            return N8nDispatchResidueOutcome.NothingToDo;

        if (execution.Status is AutomationExecutionStatus.Succeeded
            or AutomationExecutionStatus.Failed
            or AutomationExecutionStatus.Cancelled)
            return N8nDispatchResidueOutcome.NothingToDo;

        if (execution.Status == AutomationExecutionStatus.Running)
        {
            execution.Fail(
                "Dispatch attempt interrupted — provider outcome unknown — reconciliation required",
                _clock.UtcNow);
            return N8nDispatchResidueOutcome.SettledAsFailed;
        }

        return N8nDispatchResidueOutcome.RequiresOperatorSweep;
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