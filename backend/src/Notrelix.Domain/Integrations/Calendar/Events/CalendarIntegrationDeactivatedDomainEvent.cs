namespace Notrelix.Domain.Integrations.Calendar.Events;

[EventName("integrations.calendar-integration-deactivated")]
public sealed record CalendarIntegrationDeactivatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid IntegrationId,
    Guid DeactivatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
