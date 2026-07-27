namespace Notrelix.Domain.Automation.Rules.Events;

[EventName("automation.automation-rule-restored")]
public sealed record AutomationRuleRestoredDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid RuleId,
    string Name,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
