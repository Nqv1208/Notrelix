using Notrelix.Domain.Common;

namespace Notrelix.Domain.Events.Shared;

public class CommentUpdatedEvent : BaseEvent
{
    public Guid CommentId { get; }
    public Guid UpdatedBy { get; }

    public CommentUpdatedEvent(Guid commentId, Guid updatedBy)
    {
        CommentId = commentId;
        UpdatedBy = updatedBy;
    }
}
