namespace Notrelix.Domain.Governance.Permissions.Events;

public sealed record ResourcePermissionSoftDeletedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid PermissionId,
    ResourceType ResourceType,
    Guid ResourceId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, DeletedBy);
