namespace Notrelix.Domain.WorkManagement.Items.Events;

public sealed record BoardItemSoftDeletedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid ItemId,
    Guid BoardId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, DeletedBy);
