namespace Notrelix.Domain.Workspaces.Invitations.Events;

public sealed record WorkspaceInvitationResentDomainEvent(
    Guid AccountId,
    Guid InvitationId,
    Guid WorkspaceId,
    Guid ResentBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
