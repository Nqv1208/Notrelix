namespace Notrelix.Domain.WorkManagement.Boards.Events;

[EventName("work-management.board-item-identity-generated")]
public sealed record BoardItemIdentityGeneratedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid BoardId,
    long SequenceNumber,
    string ItemKey,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
