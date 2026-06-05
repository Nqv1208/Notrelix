using Notrelix.Domain.Common;

namespace Notrelix.Domain.Events.Shared;

public class CommentDeletedEvent : BaseEvent
{
    public Guid CommentId { get; }
    public Guid DeletedBy { get; }

    public CommentDeletedEvent(Guid commentId, Guid deletedBy)
    {
        CommentId = commentId;
        DeletedBy = deletedBy;
    }
}
