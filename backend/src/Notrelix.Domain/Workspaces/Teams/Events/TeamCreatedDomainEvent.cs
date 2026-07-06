namespace Notrelix.Domain.Workspaces.Teams.Events;

public sealed record TeamCreatedDomainEvent(
    Guid TeamId,
    Guid AccountId,
    Guid WorkspaceId,
    string Name,
    Guid CreatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt, CreatedBy);
