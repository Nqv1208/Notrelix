namespace Notrelix.Domain.WorkManagement.Items.Events;

public sealed record BoardItemMovedDomainEvent(
    Guid WorkspaceId,
    Guid ItemId,
    Guid BoardId,
    Guid OldGroupId,
    Guid NewGroupId,
    string NewPosition,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, UpdatedBy);
