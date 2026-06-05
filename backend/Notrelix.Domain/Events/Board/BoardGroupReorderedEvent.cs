using Notrelix.Domain.Common;

namespace Notrelix.Domain.Events.Board;

public class BoardGroupReorderedEvent : BaseEvent
{
    public Guid GroupId { get; }
    public Guid BoardId { get; }
    public double OldPosition { get; }
    public double NewPosition { get; }
    public Guid ReorderedBy { get; }

    public BoardGroupReorderedEvent(Guid groupId, Guid boardId, double oldPosition, double newPosition, Guid reorderedBy)
    {
        GroupId = groupId;
        BoardId = boardId;
        OldPosition = oldPosition;
        NewPosition = newPosition;
        ReorderedBy = reorderedBy;
    }
}
