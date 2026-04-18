using Notrelix.Domain.Common;
using Notrelix.Domain.Enums;

namespace Notrelix.Domain.Events;

// Domain Event khi member được thêm vào workspace
public class MemberAddedToWorkspaceEvent : BaseEvent
{
    public Guid WorkspaceId { get; }
    public Guid UserId { get; }
    public MemberRole Role { get; }
    public Guid AddedByUserId { get; }

    public MemberAddedToWorkspaceEvent(Guid workspaceId, Guid userId, MemberRole role, Guid addedByUserId)
    {
        WorkspaceId = workspaceId;
        UserId = userId;
        Role = role;
        AddedByUserId = addedByUserId;
    }
}
