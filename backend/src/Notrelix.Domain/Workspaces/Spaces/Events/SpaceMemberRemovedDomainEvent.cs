namespace Notrelix.Domain.Workspaces.Spaces.Events;

[EventName("workspaces.space-member-removed")]
public sealed record SpaceMemberRemovedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid SpaceId,
    SpaceMemberSubjectType SubjectType,
    Guid SubjectId,
    Guid RemovedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
