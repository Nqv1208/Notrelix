using Notrelix.Domain.Common;
using Notrelix.Domain.Enums;

namespace Notrelix.Domain.Events;

// Domain Event khi Workspace được tạo
public class WorkspaceCreatedEvent : BaseEvent
{
    public Guid WorkspaceId { get; }
    public string Name { get; }
    public WorkspaceType Type { get; }
    public Guid OwnerId { get; }

    public WorkspaceCreatedEvent(Guid workspaceId, string name, WorkspaceType type, Guid ownerId)
    {
        WorkspaceId = workspaceId;
        Name = name;
        Type = type;
        OwnerId = ownerId;
    }
}
