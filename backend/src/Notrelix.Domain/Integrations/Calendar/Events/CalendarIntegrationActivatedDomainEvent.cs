namespace Notrelix.Domain.Integrations.Calendar.Events;

public sealed record CalendarIntegrationActivatedDomainEvent(
    Guid WorkspaceId,
    Guid IntegrationId,
    Guid ActivatedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, ActivatedBy);
