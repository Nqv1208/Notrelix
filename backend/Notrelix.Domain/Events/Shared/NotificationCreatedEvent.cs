using Notrelix.Domain.Common;
using Notrelix.Domain.Enums;

namespace Notrelix.Domain.Events.Shared;

public class NotificationCreatedEvent : BaseEvent
{
    public Guid NotificationId { get; }
    public Guid WorkspaceId { get; }
    public Guid UserId { get; }
    public string Type { get; }
    public ResourceType? ResourceType { get; }
    public Guid? ResourceId { get; }

    public NotificationCreatedEvent(Guid notificationId, Guid workspaceId, Guid userId, string type, ResourceType? resourceType, Guid? resourceId)
    {
        NotificationId = notificationId;
        WorkspaceId = workspaceId;
        UserId = userId;
        Type = type;
        ResourceType = resourceType;
        ResourceId = resourceId;
    }
}
