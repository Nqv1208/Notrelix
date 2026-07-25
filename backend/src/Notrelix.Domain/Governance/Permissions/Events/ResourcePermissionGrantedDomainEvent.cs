namespace Notrelix.Domain.Governance.Permissions.Events;

public sealed record ResourcePermissionGrantedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid PermissionId,
    ResourceType ResourceType,
    Guid ResourceId,
    PermissionSubjectType Subject,
    Guid SubjectId,
    PermissionLevel Level,
    Guid GrantedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt, GrantedBy, subjectId: SubjectId);
