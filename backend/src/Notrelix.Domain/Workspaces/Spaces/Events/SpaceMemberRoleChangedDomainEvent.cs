namespace Notrelix.Domain.Workspaces.Spaces.Events;

[EventName("workspaces.space-member-role-changed")]
public sealed record SpaceMemberRoleChangedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid SpaceId,
    SpaceMemberSubjectType SubjectType,
    Guid SubjectId,
    SpaceRole OldRole,
    SpaceRole NewRole,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
