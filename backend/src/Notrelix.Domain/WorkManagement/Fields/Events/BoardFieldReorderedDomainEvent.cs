namespace Notrelix.Domain.WorkManagement.Fields.Events;

public sealed record BoardFieldReorderedDomainEvent(
    Guid WorkspaceId,
    Guid FieldId,
    Guid BoardId,
    double NewPosition,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, UpdatedBy);
