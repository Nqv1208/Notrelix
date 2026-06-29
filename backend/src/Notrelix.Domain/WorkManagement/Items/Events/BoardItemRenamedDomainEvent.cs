namespace Notrelix.Domain.WorkManagement.Items.Events;

public sealed record BoardItemRenamedDomainEvent(
    Guid WorkspaceId,
    Guid ItemId,
    Guid BoardId,
    string OldName,
    string NewName,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, UpdatedBy);
