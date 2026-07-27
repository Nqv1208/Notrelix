namespace Notrelix.Domain.Automation.Rules.Events;

[EventName("automation.automation-rule-disabled")]
public sealed record AutomationRuleDisabledDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid RuleId,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
