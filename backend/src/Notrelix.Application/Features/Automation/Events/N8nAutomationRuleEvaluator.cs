using Notrelix.Application.Features.Automation.Abstractions;
using Notrelix.Application.Events.Automation;
using Notrelix.Application.Events.WorkManagement;

namespace Notrelix.Application.Features.Automation.Events;

/// <summary>
/// Evaluates the runtime-reachable Work automation triggers ("ItemAssigned",
/// "ItemMovedToGroup", "ItemCreated") and, for each matching active rule with
/// a dispatchable action, creates an <see cref="AutomationExecution"/> and
/// stages a durable outbox intent. The intent routes explicitly by action
/// type: "MoveItem" rules stage the Work target-action dispatch (executed
/// through the Automation→Work port); "Webhook" rules stage the n8n
/// dispatch; any other configured action type fails the execution closed —
/// it is never silently rerouted to the wrong dispatcher (debt:
/// M8-AUTOMATION-ACTION-PARITY). Both dispatch intents commit atomically
/// with the consumer's transaction; the HTTP or target-action dispatch is
/// performed by a MassTransit consumer after commit. No process-local queue.
/// </summary>
public sealed class N8nAutomationRuleEvaluator
{
    private readonly IAutomationDbContext _context;
    private readonly IIntegrationEventCollector _events;
    private readonly IDateTimeProvider _clock;

    public N8nAutomationRuleEvaluator(
        IAutomationDbContext context,
        IIntegrationEventCollector events,
        IDateTimeProvider clock)
    {
        _context = context;
        _events = events;
        _clock = clock;
    }

    public async Task ExecuteAsync(
        AutomationTriggerContext trigger,
        CancellationToken cancellationToken)
    {
        // The integration event carries authoritative account/workspace scope;
        // no resource re-resolution is required at this stage.
        var workspaceId = trigger.WorkspaceId;
        var accountId = trigger.AccountId;

        var rules = await _context.AutomationRules
            .Where(rule =>
                rule.WorkspaceId == workspaceId &&
                rule.AccountId == accountId &&
                rule.Status == AutomationRuleStatus.Active)
            .ToListAsync(cancellationToken);

        foreach (var rule in rules)
        {
            if (rule.Configuration.Trigger.Type != trigger.TriggerType)
            {
                continue;
            }

            var exists = await _context.AutomationExecutions
                .AsNoTracking()
                .AnyAsync(execution =>
                    execution.RuleId == rule.Id &&
                    execution.TriggerId == trigger.SourceEventId,
                    cancellationToken);

            if (exists) continue;

            var execution = AutomationExecution.Create(
                accountId,
                workspaceId,
                rule.Id,
                trigger.SourceEventId,
                trigger.OccurredAt);

            _context.AutomationExecutions.Add(execution);

            switch (rule.Configuration.Action.Type)
            {
                case "MoveItem":
                    _events.Add(new AutomationMoveItemRequestedV1(
                        Guid.CreateVersion7(),
                        execution.Id,
                        rule.Id,
                        accountId,
                        workspaceId,
                        trigger.ActorUserId,
                        _clock.UtcNow,
                        trigger.CorrelationId,
                        trigger.CausationId));
                    break;

                case "Webhook":
                    _events.Add(new N8nDispatchRequestedV1(
                        Guid.CreateVersion7(),
                        execution.Id,
                        rule.Id,
                        accountId,
                        workspaceId,
                        _clock.UtcNow,
                        trigger.CorrelationId,
                        trigger.SourceEventId,
                        trigger.CausationId));
                    break;

                default:
                    // Fail closed: an action type without a runtime executor is
                    // never silently rerouted to another dispatcher. The rule
                    // stays a valid configuration (vocabulary is broader than
                    // the M8 runtime) but the staged execution terminates
                    // visibly instead of dispatching to the wrong channel
                    // (debt: M8-AUTOMATION-ACTION-PARITY).
                    var failedAt = _clock.UtcNow;
                    execution.Start(failedAt);
                    execution.Fail(
                        $"Action type '{rule.Configuration.Action.Type}' has no runtime executor (M8-AUTOMATION-ACTION-PARITY).",
                        failedAt);
                    break;
            }
        }
    }

    public Task ExecuteAsync(
        BoardItemMemberAssignedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        return ExecuteAsync(new AutomationTriggerContext(
            integrationEvent.AccountIdValue,
            integrationEvent.WorkspaceIdValue,
            "ItemAssigned",
            integrationEvent.SourceEventId ?? integrationEvent.EventId,
            integrationEvent.AssignedBy,
            integrationEvent.CorrelationId,
            integrationEvent.CausationId,
            integrationEvent.OccurredAt), cancellationToken);
    }

    public Task ExecuteAsync(
        BoardItemMovedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        var workspaceId = integrationEvent.WorkspaceId
            ?? throw new InvalidOperationException(
                "The moved-item fact carried no workspace scope; the tenant envelope is authoritative.");

        return ExecuteAsync(new AutomationTriggerContext(
            integrationEvent.AccountId
                ?? throw new InvalidOperationException(
                    "The moved-item fact carried no account scope; the tenant envelope is authoritative."),
            workspaceId,
            "ItemMovedToGroup",
            integrationEvent.EventId,
            integrationEvent.ActorUserId,
            integrationEvent.CorrelationId,
            integrationEvent.CausationId,
            integrationEvent.OccurredAt), cancellationToken);
    }

    public Task ExecuteAsync(
        BoardItemCreatedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        var workspaceId = integrationEvent.WorkspaceId
            ?? throw new InvalidOperationException(
                "The created-item fact carried no workspace scope; the tenant envelope is authoritative.");

        return ExecuteAsync(new AutomationTriggerContext(
            integrationEvent.AccountId
                ?? throw new InvalidOperationException(
                    "The created-item fact carried no account scope; the tenant envelope is authoritative."),
            workspaceId,
            "ItemCreated",
            integrationEvent.EventId,
            integrationEvent.ActorUserId,
            integrationEvent.CorrelationId,
            integrationEvent.CausationId,
            integrationEvent.OccurredAt), cancellationToken);
    }
}
