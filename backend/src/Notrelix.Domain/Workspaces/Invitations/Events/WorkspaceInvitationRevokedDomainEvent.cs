namespace Notrelix.Domain.Workspaces.Invitations.Events;

public sealed record WorkspaceInvitationRevokedDomainEvent(
    Guid InvitationId,
    Guid WorkspaceId,
    Guid RevokedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, RevokedBy);
