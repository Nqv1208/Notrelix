namespace Notrelix.Domain.Governance.Permissions.Events;

public sealed record ResourcePermissionLevelChangedDomainEvent(
    Guid WorkspaceId,
    Guid PermissionId,
    ResourceType ResourceType,
    Guid ResourceId,
    PermissionSubjectType SubjectType,
    Guid SubjectId,
    PermissionLevel OldLevel,
    PermissionLevel NewLevel,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, UpdatedBy);
