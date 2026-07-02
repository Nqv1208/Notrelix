namespace Notrelix.Domain.Workspaces.Teams.Events;

public sealed record TeamArchivedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid TeamId,
    Guid ArchivedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt, ArchivedBy);
