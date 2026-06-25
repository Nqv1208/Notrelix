namespace Notrelix.Domain.WorkManagement.Boards.Events;

public sealed record BoardRenamedDomainEvent(
    Guid WorkspaceId,
    Guid BoardId,
    string OldTitle,
    string NewTitle,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, UpdatedBy);
