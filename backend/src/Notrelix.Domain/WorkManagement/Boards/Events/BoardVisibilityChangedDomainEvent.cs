namespace Notrelix.Domain.WorkManagement.Boards.Events;

public sealed record BoardVisibilityChangedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid BoardId,
    BoardVisibility OldVisibility,
    BoardVisibility NewVisibility,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
