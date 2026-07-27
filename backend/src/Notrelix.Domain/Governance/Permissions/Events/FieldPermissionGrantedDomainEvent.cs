namespace Notrelix.Domain.Governance.Permissions.Events;

[EventName("governance.field-permission-granted")]
public sealed record FieldPermissionGrantedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid FieldId,
    PermissionSubjectType Subject,
    Guid SubjectId,
    PermissionLevel Level,
    Guid GrantedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
