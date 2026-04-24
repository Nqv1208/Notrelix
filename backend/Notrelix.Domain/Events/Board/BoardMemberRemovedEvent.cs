using Notrelix.Domain.Common;

namespace Notrelix.Domain.Events.Board;

public class BoardMemberRemovedEvent : BaseEvent
{
    public Guid BoardId { get; }
    public Guid UserId { get; }

    public BoardMemberRemovedEvent(Guid boardId, Guid userId)
    {
        BoardId = boardId;
        UserId = userId;
    }
}
