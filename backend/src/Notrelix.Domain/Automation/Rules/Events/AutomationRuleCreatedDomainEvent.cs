namespace Notrelix.Domain.Automation.Rules.Events;

public sealed record AutomationRuleCreatedDomainEvent(
    Guid WorkspaceId,
    Guid RuleId,
    string Name,
    Guid CreatedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, CreatedBy);
