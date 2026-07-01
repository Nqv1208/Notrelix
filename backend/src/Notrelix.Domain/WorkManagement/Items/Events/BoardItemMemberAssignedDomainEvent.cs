namespace Notrelix.Domain.WorkManagement.Items.Events;

public sealed record BoardItemMemberAssignedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid ItemId,
    Guid UserId,
    Guid AssignedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, AssignedBy);
