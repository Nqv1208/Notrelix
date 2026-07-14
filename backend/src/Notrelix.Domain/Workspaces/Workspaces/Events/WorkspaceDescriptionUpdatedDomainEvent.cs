namespace Notrelix.Domain.Workspaces.Workspaces.Events;

public sealed record WorkspaceDescriptionUpdatedDomainEvent(
    Guid WorkspaceId,
    string? OldDescription,
    string? NewDescription,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceRootDomainEvent(WorkspaceId, OccurredAt, UpdatedBy);
