namespace Notrelix.Domain.Workspaces.Teams.Events;

public sealed record TeamDescriptionUpdatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid TeamId,
    string? OldDescription,
    string? NewDescription,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt, UpdatedBy);
