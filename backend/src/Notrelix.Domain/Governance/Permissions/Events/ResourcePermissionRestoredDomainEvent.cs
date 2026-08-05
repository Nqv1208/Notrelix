namespace Notrelix.Domain.Governance.Permissions.Events;

[EventName("governance.resource-permission-restored")]
public sealed record ResourcePermissionRestoredDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid PermissionId,
    ResourceKind ResourceKind,
    Guid ResourceId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
