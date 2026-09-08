using Notrelix.Application.Features.Automation.Abstractions;
using Notrelix.Application.Events.Automation;
using Notrelix.Application.Events.WorkManagement;

namespace Notrelix.Application.Features.Automation.Events;

/// <summary>
/// Evaluates the accepted "board item member assigned" automation trigger and,
/// for each matching active webhook rule, creates an <see cref="AutomationExecution"/>
/// and stages a durable <see cref="N8nDispatchRequestedV1"/> outbox intent.
/// Both commit atomically with the consumer's transaction; the n8n HTTP dispatch
/// is performed by a MassTransit consumer after commit. No process-local queue.
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
        BoardItemMemberAssignedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        // The integration event carries authoritative account/workspace scope;
        // no resource re-resolution is required at this stage.
        var workspaceId = integrationEvent.WorkspaceIdValue;
        var accountId = integrationEvent.AccountIdValue;

        var rules = await _context.AutomationRules
            .Where(rule =>
                rule.WorkspaceId == workspaceId &&
                rule.Status == AutomationRuleStatus.Active)
            .ToListAsync(cancellationToken);

        foreach (var rule in rules)
        {
            if (rule.Configuration.Trigger.Type != "ItemAssigned" ||
                rule.Configuration.Action.Type != "Webhook")
            {
                continue;
            }

            var exists = await _context.AutomationExecutions
                .AsNoTracking()
                .AnyAsync(execution =>
                    execution.RuleId == rule.Id &&
                    execution.TriggerId == integrationEvent.SourceEventId,
                    cancellationToken);

            if (exists) continue;

            var execution = AutomationExecution.Create(
                accountId,
                workspaceId,
                rule.Id,
                integrationEvent.SourceEventId ?? integrationEvent.EventId,
                integrationEvent.OccurredAt);

            _context.AutomationExecutions.Add(execution);

            _events.Add(new N8nDispatchRequestedV1(
                Guid.CreateVersion7(),
                execution.Id,
                rule.Id,
                accountId,
                workspaceId,
                _clock.UtcNow,
                integrationEvent.CorrelationId,
                integrationEvent.EventId,
                integrationEvent.CausationId));
        }
    }
}
