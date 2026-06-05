using Notrelix.Domain.Common;
using Notrelix.Domain.Enums;

namespace Notrelix.Domain.Events.Shared;

public class ActivityLogCreatedEvent : BaseEvent
{
    public Guid ActivityLogId { get; }
    public Guid WorkspaceId { get; }
    public Guid ActorId { get; }
    public string Action { get; }
    public ResourceType ResourceType { get; }
    public Guid ResourceId { get; }

    public ActivityLogCreatedEvent(Guid activityLogId, Guid workspaceId, Guid actorId, string action, ResourceType resourceType, Guid resourceId)
    {
        ActivityLogId = activityLogId;
        WorkspaceId = workspaceId;
        ActorId = actorId;
        Action = action;
        ResourceType = resourceType;
        ResourceId = resourceId;
    }
}
