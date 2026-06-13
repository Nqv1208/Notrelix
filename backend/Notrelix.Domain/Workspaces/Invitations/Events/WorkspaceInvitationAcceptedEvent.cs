using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Workspaces.Invitations.Events;

public sealed record WorkspaceInvitationAcceptedEvent(
    Guid InvitationId,
    Guid WorkspaceId,
    Guid UserId,
    Guid AcceptedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
