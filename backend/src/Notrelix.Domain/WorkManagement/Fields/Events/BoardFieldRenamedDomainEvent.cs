namespace Notrelix.Domain.WorkManagement.Fields.Events;

public sealed record BoardFieldRenamedDomainEvent(
    Guid WorkspaceId,
    Guid FieldId,
    Guid BoardId,
    string OldName,
    string NewName,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, UpdatedBy);
