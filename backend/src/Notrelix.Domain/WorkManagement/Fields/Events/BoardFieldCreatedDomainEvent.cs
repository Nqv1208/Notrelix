namespace Notrelix.Domain.WorkManagement.Fields.Events;

public sealed record BoardFieldCreatedDomainEvent(
    Guid WorkspaceId,
    Guid BoardId,
    Guid FieldId,
    string Name,
    FieldType Type,
    Guid CreatedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, CreatedBy);
