namespace Notrelix.Domain.Governance.Permissions.Events;

public sealed record ResourcePermissionRevokedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid PermissionId,
    ResourceType ResourceType,
    Guid ResourceId,
    PermissionSubjectType Subject,
    Guid SubjectId,
    Guid RevokedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, RevokedBy);
