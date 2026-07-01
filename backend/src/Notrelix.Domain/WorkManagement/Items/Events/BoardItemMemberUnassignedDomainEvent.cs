namespace Notrelix.Domain.WorkManagement.Items.Events;

public sealed record BoardItemMemberUnassignedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid ItemId,
    Guid UserId,
    Guid UnassignedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, UnassignedBy);
