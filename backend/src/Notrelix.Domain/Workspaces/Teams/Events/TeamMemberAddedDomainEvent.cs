namespace Notrelix.Domain.Workspaces.Teams.Events;

public sealed record TeamMemberAddedDomainEvent(
    Guid WorkspaceId,
    Guid TeamId,
    Guid UserId,
    TeamMemberRole Role,
    Guid AddedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, AddedBy);
