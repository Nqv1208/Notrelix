using Notrelix.Domain.Common;

namespace Notrelix.Domain.Workspaces.Members;

public sealed record WorkspaceMemberRemovedEvent(
    Guid WorkspaceId,
    Guid MemberId,
    Guid UserId,
    Guid ActorId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
