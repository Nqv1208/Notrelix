namespace Notrelix.Domain.WorkManagement.Items.Events;

public sealed record BoardItemParentAssignedDomainEvent(
    Guid WorkspaceId,
    Guid BoardId,
    Guid ItemId,
    Guid? ParentItemId,
    int ItemLevel,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, UpdatedBy);
