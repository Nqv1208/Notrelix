namespace Notrelix.Domain.Workspaces.Workspaces.Events;

public sealed record WorkspaceSettingsUpdatedDomainEvent(
    Guid WorkspaceId,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceRootDomainEvent(WorkspaceId, OccurredAt, UpdatedBy);
