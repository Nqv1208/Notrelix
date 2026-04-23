using Notrelix.Domain.Common;

namespace Notrelix.Domain.Events.Workspace;

public class WorkspaceCreatedEvent : BaseEvent
{
    public Guid WorkspaceId { get; }
    public string Name { get; }
    public Guid OwnerId { get; }
    public bool IsPersonal { get; }

    public WorkspaceCreatedEvent(Guid workspaceId, string name, Guid ownerId, bool isPersonal)
    {
        WorkspaceId = workspaceId;
        Name = name;
        OwnerId = ownerId;
        IsPersonal = isPersonal;
    }
}
