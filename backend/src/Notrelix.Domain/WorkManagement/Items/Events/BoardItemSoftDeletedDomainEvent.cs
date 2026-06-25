namespace Notrelix.Domain.WorkManagement.Items.Events;

public sealed record BoardItemSoftDeletedDomainEvent(
    Guid WorkspaceId,
    Guid ItemId,
    Guid BoardId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, DeletedBy);
