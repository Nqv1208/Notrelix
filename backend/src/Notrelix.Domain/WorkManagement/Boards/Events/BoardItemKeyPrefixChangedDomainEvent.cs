namespace Notrelix.Domain.WorkManagement.Boards.Events;

[EventName("work-management.board-item-key-prefix-changed")]
public sealed record BoardItemKeyPrefixChangedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid BoardId,
    string? OldPrefix,
    string NewPrefix,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
