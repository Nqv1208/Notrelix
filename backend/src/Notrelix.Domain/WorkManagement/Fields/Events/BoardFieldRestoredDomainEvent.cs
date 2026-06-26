namespace Notrelix.Domain.WorkManagement.Fields.Events;

public sealed record BoardFieldRestoredDomainEvent(
    Guid WorkspaceId,
    Guid BoardId,
    Guid FieldId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, RestoredBy);
