namespace Notrelix.Domain.WorkManagement.Items.Events;

public sealed record BoardItemMemberAssignedDomainEvent(
    Guid WorkspaceId,
    Guid ItemId,
    Guid UserId,
    Guid AssignedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, AssignedBy);
