using Notrelix.Application.Common.Events;
using Notrelix.Application.Features.Automation.Abstractions;
using Notrelix.Application.Features.Automation.Jobs;

namespace Notrelix.Application.Features.Automation.Events;

public class CardAssignedN8nAutomationHandler : INotificationHandler<DomainEventNotification<BoardItemMemberAssignedDomainEvent>>
{
    private readonly IAutomationDbContext _context;
    private readonly IResourceReferenceResolver _resourceResolver;
    private readonly IJobQueue _jobQueue;

    public CardAssignedN8nAutomationHandler(IAutomationDbContext context, IResourceReferenceResolver resourceResolver, IJobQueue jobQueue)
    {
        _context = context;
        _resourceResolver = resourceResolver;
        _jobQueue = jobQueue;
    }

    public async Task Handle(DomainEventNotification<BoardItemMemberAssignedDomainEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        var boardItemContext = await _resourceResolver.GetAccountContextAsync(domainEvent.ItemId, ResourceTypes.BoardItem, cancellationToken);
        if (boardItemContext is null) return;

        var rules = await _context.AutomationRules
            .Where(rule =>
                rule.WorkspaceId == boardItemContext.WorkspaceId &&
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
                boardItemContext.AccountId,
                boardItemContext.WorkspaceId,
                rule.Id,
                domainEvent.EventId,
                domainEvent.OccurredAt);

            _context.AutomationExecutions.Add(execution);
            await _context.SaveChangesAsync(cancellationToken);

            await _jobQueue.EnqueueAsync(new N8nDispatchJob(execution.Id, rule.Id), cancellationToken);
        }
    }
}
