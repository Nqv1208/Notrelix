namespace Notrelix.Domain.Integrations.Calendar.Events;

public sealed record CalendarIntegrationDeactivatedDomainEvent(
    Guid WorkspaceId,
    Guid IntegrationId,
    Guid DeactivatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, DeactivatedBy);
