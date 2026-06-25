namespace Notrelix.Domain.Automation.Rules.Events;

public sealed record AutomationRuleDeletedDomainEvent(
    Guid WorkspaceId,
    Guid RuleId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, DeletedBy);
