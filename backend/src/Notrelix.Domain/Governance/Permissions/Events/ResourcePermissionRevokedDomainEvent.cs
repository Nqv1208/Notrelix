namespace Notrelix.Domain.Governance.Permissions.Events;

[EventName("governance.resource-permission-revoked")]
public sealed record ResourcePermissionRevokedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid PermissionId,
    ResourceKind ResourceKind,
    Guid ResourceId,
    PermissionSubjectType Subject,
    Guid SubjectId,
    Guid RevokedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
