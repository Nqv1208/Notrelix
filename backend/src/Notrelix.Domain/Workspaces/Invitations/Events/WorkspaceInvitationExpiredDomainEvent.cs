namespace Notrelix.Domain.Workspaces.Invitations.Events;

public sealed record WorkspaceInvitationExpiredDomainEvent(
    Guid InvitationId,
    Guid WorkspaceId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, null);
