namespace Notrelix.Domain.Automation.Rules.Events;

public sealed record AutomationRuleRestoredDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid RuleId,
    string Name,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
