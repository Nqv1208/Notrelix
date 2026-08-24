using Notrelix.Application.Features.Automation.Abstractions;
using Notrelix.Application.Features.Automation.Jobs;
using Notrelix.Application.Events.Automation;

namespace Notrelix.Application.Features.Automation.Events;

public sealed class CheckN8nAutomationPostCommitAction
{
    private readonly IAutomationDbContext _context;
    private readonly IResourceReferenceResolver _resourceResolver;
    private readonly IJobQueue _jobQueue;

    public CheckN8nAutomationPostCommitAction(
        IAutomationDbContext context,
        IResourceReferenceResolver resourceResolver,
        IJobQueue jobQueue)
    {
        _context = context;
        _resourceResolver = resourceResolver;
        _jobQueue = jobQueue;
    }

    public async Task ExecuteAsync(BoardItemMemberAssignedForAutomationIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        var boardItemContext = await _resourceResolver.GetAccountContextAsync(
            integrationEvent.ItemId, ResourceTypes.BoardItem, cancellationToken);
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
                    execution.TriggerId == integrationEvent.SourceEventId,
                    cancellationToken);

            if (exists) continue;

            var execution = AutomationExecution.Create(
                boardItemContext.AccountId,
                boardItemContext.WorkspaceId,
                rule.Id,
                integrationEvent.SourceEventId ?? integrationEvent.EventId,
                integrationEvent.OccurredAt);

            _context.AutomationExecutions.Add(execution);
            await _context.SaveChangesAsync(cancellationToken);

            await _jobQueue.EnqueueAsync(new N8nDispatchJob(execution.Id, rule.Id), cancellationToken);
        }
    }
}
