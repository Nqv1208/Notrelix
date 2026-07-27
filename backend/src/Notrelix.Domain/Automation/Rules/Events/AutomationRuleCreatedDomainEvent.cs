namespace Notrelix.Domain.Automation.Rules.Events;

[EventName("automation.automation-rule-created")]
public sealed record AutomationRuleCreatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid RuleId,
    string Name,
    Guid CreatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
