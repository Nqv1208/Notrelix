namespace Notrelix.Domain.WorkManagement.Boards.Events;

public sealed record BoardUnarchivedDomainEvent(
    Guid WorkspaceId,
    Guid BoardId,
    Guid UnarchivedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, UnarchivedBy);
