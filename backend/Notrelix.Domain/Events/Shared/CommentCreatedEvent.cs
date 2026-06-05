using Notrelix.Domain.Common;
using Notrelix.Domain.Enums;

namespace Notrelix.Domain.Events.Shared;

public class CommentCreatedEvent : BaseEvent
{
    public Guid CommentId { get; }
    public Guid WorkspaceId { get; }
    public ResourceType ResourceType { get; }
    public Guid ResourceId { get; }
    public Guid CreatedBy { get; }

    public CommentCreatedEvent(Guid commentId, Guid workspaceId, ResourceType resourceType, Guid resourceId, Guid createdBy)
    {
        CommentId = commentId;
        WorkspaceId = workspaceId;
        ResourceType = resourceType;
        ResourceId = resourceId;
        CreatedBy = createdBy;
    }
}
