namespace Notrelix.Domain.Governance.Permissions.Events;

[EventName("governance.resource-permission-level-changed")]
public sealed record ResourcePermissionLevelChangedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid PermissionId,
    ResourceType ResourceType,
    Guid ResourceId,
    PermissionSubjectType Subject,
    Guid SubjectId,
    PermissionLevel OldLevel,
    PermissionLevel NewLevel,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
