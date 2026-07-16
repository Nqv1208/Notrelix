namespace Notrelix.Domain.Governance.Permissions.Events;

public sealed record FieldPermissionGrantedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid FieldId,
    PermissionSubjectType Subject,
    Guid SubjectId,
    PermissionLevel Level,
    Guid GrantedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt, GrantedBy, subjectId: SubjectId);
