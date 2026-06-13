using Notrelix.Domain.Common;

namespace Notrelix.Domain.Workspaces.Members.Events;

public sealed record WorkspaceMemberSuspendedEvent(
    Guid WorkspaceId,
    Guid MemberId,
    Guid UserId,
    Guid ActorId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
