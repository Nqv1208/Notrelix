using Notrelix.Domain.Common;

namespace Notrelix.Domain.Workspaces.Members.Events;

public sealed record WorkspaceMemberAddedEvent(
    Guid WorkspaceId,
    Guid MemberId,
    Guid UserId,
    WorkspaceRole Role,
    Guid ActorId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
