namespace Notrelix.Domain.Integrations.Calendar.Events;

public sealed record CalendarIntegrationSyncDirectionChangedDomainEvent(
    Guid WorkspaceId,
    Guid IntegrationId,
    CalendarSyncDirection NewDirection,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, UpdatedBy);
