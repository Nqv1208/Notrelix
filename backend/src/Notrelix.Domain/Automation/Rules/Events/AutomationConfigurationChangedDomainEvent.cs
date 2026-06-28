namespace Notrelix.Domain.Automation.Rules.Events;

public sealed record AutomationConfigurationChangedDomainEvent(
    Guid WorkspaceId,
    Guid RuleId,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, UpdatedBy);
