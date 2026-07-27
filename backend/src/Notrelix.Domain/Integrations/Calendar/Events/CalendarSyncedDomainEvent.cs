namespace Notrelix.Domain.Integrations.Calendar.Events;

[EventName("integrations.calendar-synced")]
public sealed record CalendarSyncedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid IntegrationId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
