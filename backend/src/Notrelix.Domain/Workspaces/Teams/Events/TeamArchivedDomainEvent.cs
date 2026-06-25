namespace Notrelix.Domain.Workspaces.Teams.Events;

public sealed record TeamArchivedDomainEvent(
    Guid WorkspaceId,
    Guid TeamId,
    Guid ArchivedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, ArchivedBy);
