namespace Notrelix.Domain.Integrations.Calendar.Events;

public sealed record CalendarSyncedDomainEvent(
    Guid WorkspaceId,
    Guid IntegrationId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, null);
