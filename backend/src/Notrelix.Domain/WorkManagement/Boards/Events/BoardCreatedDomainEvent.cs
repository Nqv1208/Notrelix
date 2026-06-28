namespace Notrelix.Domain.WorkManagement.Boards.Events;

public sealed record BoardCreatedDomainEvent(
    Guid WorkspaceId,
    Guid BoardId,
    string Title,
    Guid CreatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, CreatedBy);
