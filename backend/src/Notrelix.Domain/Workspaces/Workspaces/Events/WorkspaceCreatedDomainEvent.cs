namespace Notrelix.Domain.Workspaces.Workspaces.Events;

public sealed record WorkspaceCreatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    string Name,
    string Slug,
    Guid CreatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceRootDomainEvent(WorkspaceId, OccurredAt, CreatedBy);
