namespace Notrelix.Domain.Automation.Rules.Events;

public sealed record AutomationRuleDisabledDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid RuleId,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, UpdatedBy);
