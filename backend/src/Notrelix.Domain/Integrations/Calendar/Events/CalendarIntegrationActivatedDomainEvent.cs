namespace Notrelix.Domain.Integrations.Calendar.Events;

public sealed record CalendarIntegrationActivatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid IntegrationId,
    Guid ActivatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, ActivatedBy);
