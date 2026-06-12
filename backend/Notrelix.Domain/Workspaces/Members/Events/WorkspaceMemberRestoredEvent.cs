using Notrelix.Domain.Common;

namespace Notrelix.Domain.Workspaces.Members.Events;

public sealed record WorkspaceMemberRestoredEvent(
    Guid WorkspaceId,
    Guid MemberId,
    Guid UserId,
    Guid RestoredBy,
    DateTimeOffset RestoredAt
) : DomainEvent(RestoredAt);
