namespace Notrelix.Domain.Automation.Rules.Events;

[EventName("automation.automation-rule-enabled")]
public sealed record AutomationRuleEnabledDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid RuleId,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
