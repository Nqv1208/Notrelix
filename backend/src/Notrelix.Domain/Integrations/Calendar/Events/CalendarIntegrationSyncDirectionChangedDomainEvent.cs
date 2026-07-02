namespace Notrelix.Domain.Integrations.Calendar.Events;

public sealed record CalendarIntegrationSyncDirectionChangedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid IntegrationId,
    CalendarSyncDirection NewDirection,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt, UpdatedBy);
