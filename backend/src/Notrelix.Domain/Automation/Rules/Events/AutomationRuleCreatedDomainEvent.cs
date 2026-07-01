namespace Notrelix.Domain.Automation.Rules.Events;

public sealed record AutomationRuleCreatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid RuleId,
    string Name,
    Guid CreatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, CreatedBy);
