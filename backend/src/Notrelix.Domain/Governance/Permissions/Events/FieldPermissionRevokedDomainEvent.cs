namespace Notrelix.Domain.Governance.Permissions.Events;

public sealed record FieldPermissionRevokedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid FieldId,
    PermissionSubjectType Subject,
    Guid SubjectId,
    Guid RevokedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, RevokedBy);
