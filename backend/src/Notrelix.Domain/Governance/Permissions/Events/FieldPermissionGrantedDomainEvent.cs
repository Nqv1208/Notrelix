namespace Notrelix.Domain.Governance.Permissions.Events;

public sealed record FieldPermissionGrantedDomainEvent(
    Guid WorkspaceId,
    Guid FieldId,
    PermissionSubjectType SubjectType,
    Guid SubjectId,
    PermissionLevel Level,
    Guid GrantedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, GrantedBy);
