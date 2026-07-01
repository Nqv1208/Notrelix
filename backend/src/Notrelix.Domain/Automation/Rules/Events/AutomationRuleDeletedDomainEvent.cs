namespace Notrelix.Domain.Automation.Rules.Events;

public sealed record AutomationRuleDeletedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid RuleId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, DeletedBy);
