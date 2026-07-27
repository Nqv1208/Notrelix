using Notrelix.Domain.Workspaces.Members;
namespace Notrelix.Domain.Workspaces.Invitations.Events;

[EventName("workspaces.workspace-invitation-created")]
public sealed record WorkspaceInvitationCreatedDomainEvent(
    Guid AccountId,
    Guid InvitationId,
    Guid WorkspaceId,
    string Email,
    WorkspaceRole Role,
    Guid InvitedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
