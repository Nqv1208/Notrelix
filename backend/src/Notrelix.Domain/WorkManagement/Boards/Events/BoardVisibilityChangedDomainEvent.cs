namespace Notrelix.Domain.WorkManagement.Boards.Events;

[EventName("work-management.board-visibility-changed")]
public sealed record BoardVisibilityChangedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid BoardId,
    BoardVisibility OldVisibility,
    BoardVisibility NewVisibility,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
