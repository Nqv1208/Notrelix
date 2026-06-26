namespace Notrelix.Domain.WorkManagement.Fields.Events;

public sealed record BoardFieldUpdatedDomainEvent(
    Guid WorkspaceId,
    Guid FieldId,
    Guid BoardId,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, UpdatedBy);
