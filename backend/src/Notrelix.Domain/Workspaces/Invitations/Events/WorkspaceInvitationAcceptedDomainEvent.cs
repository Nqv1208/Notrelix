namespace Notrelix.Domain.Workspaces.Invitations.Events;

public sealed record WorkspaceInvitationAcceptedDomainEvent(
    Guid InvitationId,
    Guid WorkspaceId,
    Guid UserId,
    Guid AcceptedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, AcceptedBy);
