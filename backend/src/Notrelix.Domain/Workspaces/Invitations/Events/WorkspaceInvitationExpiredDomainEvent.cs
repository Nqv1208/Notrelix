namespace Notrelix.Domain.Workspaces.Invitations.Events;

public sealed record WorkspaceInvitationExpiredDomainEvent(
    Guid AccountId,
    Guid InvitationId,
    Guid WorkspaceId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
