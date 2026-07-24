namespace Notrelix.Domain.Automation.Rules.Events;

[EventName("automation.automation-rule-deleted")]
public sealed record AutomationRuleDeletedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid RuleId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
