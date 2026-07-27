namespace Notrelix.Domain.Governance.Permissions.Events;

[EventName("governance.field-permission-revoked")]
public sealed record FieldPermissionRevokedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid FieldId,
    PermissionSubjectType Subject,
    Guid SubjectId,
    Guid RevokedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
