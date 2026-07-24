namespace Notrelix.Domain.WorkManagement.Boards.Events;

[EventName("work-management.board-description-updated")]
public sealed record BoardDescriptionUpdatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid BoardId,
    string? OldDescription,
    string? NewDescription,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
