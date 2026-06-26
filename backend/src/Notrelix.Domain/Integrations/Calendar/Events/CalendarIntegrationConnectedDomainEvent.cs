namespace Notrelix.Domain.Integrations.Calendar.Events;

public sealed record CalendarIntegrationConnectedDomainEvent(
    Guid WorkspaceId,
    Guid ConnectionId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, null);
