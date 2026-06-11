using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Workspaces.Invitations;

public sealed record WorkspaceInvitationRevokedEvent(
    Guid InvitationId,
    Guid WorkspaceId,
    Guid RevokedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
