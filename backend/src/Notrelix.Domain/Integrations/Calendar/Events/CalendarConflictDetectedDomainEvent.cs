namespace Notrelix.Domain.Integrations.Calendar.Events;

public sealed record CalendarConflictDetectedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid IntegrationId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
