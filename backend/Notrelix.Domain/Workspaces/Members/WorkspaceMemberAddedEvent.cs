using Notrelix.Domain.Common;

namespace Notrelix.Domain.Workspaces.Members;

public sealed record WorkspaceMemberAddedEvent(
    Guid WorkspaceId,
    Guid MemberId,
    Guid UserId,
    WorkspaceRole Role,
    Guid ActorId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
