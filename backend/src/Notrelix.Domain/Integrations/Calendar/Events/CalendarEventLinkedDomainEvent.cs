namespace Notrelix.Domain.Integrations.Calendar.Events;

[EventName("integrations.calendar-event-linked")]
public sealed record CalendarEventLinkedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid IntegrationId,
    Guid CalendarEventId,
    ResourceRef Target,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
