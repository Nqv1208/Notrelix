namespace Notrelix.Domain.Integrations.Calendar.Events;

public sealed record CalendarEventLinkedDomainEvent(
    Guid WorkspaceId,
    Guid IntegrationId,
    Guid CalendarEventId,
    ResourceRef Target,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, null);
