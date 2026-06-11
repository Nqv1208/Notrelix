using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Workspaces.Invitations;

public sealed record WorkspaceInvitationCreatedEvent(
    Guid InvitationId,
    Guid WorkspaceId,
    string Email,
    WorkspaceRole Role,
    Guid InvitedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
