namespace Notrelix.Domain.Workspaces.Spaces.Events;

[EventName("workspaces.space-member-added")]
public sealed record SpaceMemberAddedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid SpaceId,
    SpaceMemberSubjectType SubjectType,
    Guid SubjectId,
    SpaceRole Role,
    Guid AddedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
