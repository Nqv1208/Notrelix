using Notrelix.Domain.Common;

namespace Notrelix.Domain.Workspaces.Invitations;

public record WorkspaceInvitationCreatedEvent(Guid InvitationId, Guid WorkspaceId, string Email, WorkspaceRole Role, Guid InvitedBy) : DomainRecordEvent;
public record WorkspaceInvitationAcceptedEvent(Guid InvitationId, Guid WorkspaceId, Guid UserId, Guid AcceptedBy) : DomainRecordEvent;
public record WorkspaceInvitationRevokedEvent(Guid InvitationId, Guid WorkspaceId, Guid RevokedBy) : DomainRecordEvent;
public record WorkspaceInvitationExpiredEvent(Guid InvitationId, Guid WorkspaceId) : DomainRecordEvent;
