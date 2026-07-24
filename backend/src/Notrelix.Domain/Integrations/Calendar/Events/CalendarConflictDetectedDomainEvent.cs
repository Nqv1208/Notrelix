namespace Notrelix.Domain.Integrations.Calendar.Events;

[EventName("integrations.calendar-conflict-detected")]
public sealed record CalendarConflictDetectedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid IntegrationId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
