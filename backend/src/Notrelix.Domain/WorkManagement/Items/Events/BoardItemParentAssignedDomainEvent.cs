namespace Notrelix.Domain.WorkManagement.Items.Events;

public sealed record BoardItemParentAssignedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid BoardId,
    Guid ItemId,
    Guid? ParentItemId,
    int ItemLevel,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, UpdatedBy);
