namespace Notrelix.Domain.WorkManagement.Boards.Events;

public sealed record BoardDescriptionUpdatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid BoardId,
    string? OldDescription,
    string? NewDescription,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, UpdatedBy);
