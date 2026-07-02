namespace Notrelix.Domain.Integrations.Calendar.Events;

public sealed record CalendarIntegrationDeactivatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid IntegrationId,
    Guid DeactivatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt, DeactivatedBy);
