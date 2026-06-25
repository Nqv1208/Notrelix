namespace Notrelix.Domain.Workspaces.Workspaces.Events;

public sealed record WorkspaceArchivedDomainEvent(
    Guid WorkspaceId,
    Guid ArchivedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, ArchivedBy);
