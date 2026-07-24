namespace Notrelix.Domain.Integrations.Calendar.Events;

[EventName("integrations.calendar-integration-activated")]
public sealed record CalendarIntegrationActivatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid IntegrationId,
    Guid ActivatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
