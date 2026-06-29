namespace Notrelix.Domain.Automation.Rules.Events;

public sealed record AutomationRuleRestoredDomainEvent(
    Guid WorkspaceId,
    Guid RuleId,
    string Name,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, RestoredBy);
