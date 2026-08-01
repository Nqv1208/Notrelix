namespace Notrelix.Domain.Governance.Permissions.Events;

[EventName("governance.resource-permission-deleted")]
public sealed record ResourcePermissionDeletedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid PermissionId,
    ResourceType ResourceType,
    Guid ResourceId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
