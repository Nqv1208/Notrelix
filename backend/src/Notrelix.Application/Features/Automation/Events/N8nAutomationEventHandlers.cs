using Notrelix.Application.Common.Events;
using Notrelix.Application.Features.Automation.Jobs;

namespace Notrelix.Application.Features.Automation.Events;

public class CardAssignedN8nAutomationHandler : INotificationHandler<DomainEventNotification<BoardItemMemberAssignedDomainEvent>>
{
    private readonly IApplicationDbContext _context;
    private readonly IJobQueue _jobQueue;

    public CardAssignedN8nAutomationHandler(IApplicationDbContext context, IJobQueue jobQueue)
    {
        _context = context;
        _jobQueue = jobQueue;
    }

    public async Task Handle(DomainEventNotification<BoardItemMemberAssignedDomainEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        var card = await _context.BoardItems
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == domainEvent.ItemId && c.DeletedAt == null, cancellationToken);

        if (card is null) return;

        var rules = await _context.AutomationRules
            .Where(rule =>
                rule.WorkspaceId == card.WorkspaceId &&
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
                    execution.TriggerId == domainEvent.EventId,
                    cancellationToken);

            if (exists) continue;

            var execution = AutomationExecution.Create(
                Guid.Empty,
                card.WorkspaceId,
                rule.Id,
                domainEvent.EventId,
                domainEvent.OccurredAt);

            _context.AutomationExecutions.Add(execution);
            await _context.SaveChangesAsync(cancellationToken);

            await _jobQueue.EnqueueAsync(new N8nDispatchJob(execution.Id, rule.Id), cancellationToken);
        }
    }
}
