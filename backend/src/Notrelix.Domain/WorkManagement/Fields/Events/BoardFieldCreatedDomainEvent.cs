namespace Notrelix.Domain.WorkManagement.Fields.Events;

public sealed record BoardFieldCreatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid BoardId,
    Guid FieldId,
    string Name,
    FieldType Type,
    Guid CreatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, CreatedBy);
