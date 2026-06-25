namespace Notrelix.Domain.Workspaces.Invitations.Events;

public sealed record WorkspaceInvitationCreatedDomainEvent(
    Guid InvitationId,
    Guid WorkspaceId,
    string Email,
    WorkspaceRole Role,
    Guid InvitedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, InvitedBy);
