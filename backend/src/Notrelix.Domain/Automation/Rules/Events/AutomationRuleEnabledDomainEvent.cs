namespace Notrelix.Domain.Automation.Rules.Events;

public sealed record AutomationRuleEnabledDomainEvent(
    Guid WorkspaceId,
    Guid RuleId,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, UpdatedBy);
