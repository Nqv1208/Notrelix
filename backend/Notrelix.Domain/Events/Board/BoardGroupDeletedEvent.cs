using Notrelix.Domain.Common;

namespace Notrelix.Domain.Events.Board;

public class BoardGroupDeletedEvent : BaseEvent
{
    public Guid GroupId { get; }
    public Guid BoardId { get; }
    public Guid DeletedBy { get; }

    public BoardGroupDeletedEvent(Guid groupId, Guid boardId, Guid deletedBy)
    {
        GroupId = groupId;
        BoardId = boardId;
        DeletedBy = deletedBy;
    }
}
