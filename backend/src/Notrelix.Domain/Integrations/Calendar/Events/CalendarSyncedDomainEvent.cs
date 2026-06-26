namespace Notrelix.Domain.Integrations.Calendar.Events;

public sealed record CalendarSyncedDomainEvent(
    Guid WorkspaceId,
    Guid IntegrationId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, null);
