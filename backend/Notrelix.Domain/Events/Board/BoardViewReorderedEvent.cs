using Notrelix.Domain.Common;

namespace Notrelix.Domain.Events.Board;

public class BoardViewReorderedEvent : BaseEvent
{
    public Guid BoardViewId { get; }
    public Guid BoardId { get; }
    public double OldPosition { get; }
    public double NewPosition { get; }
    public Guid ReorderedBy { get; }

    public BoardViewReorderedEvent(Guid boardViewId, Guid boardId, double oldPosition, double newPosition, Guid reorderedBy)
    {
        BoardViewId = boardViewId;
        BoardId = boardId;
        OldPosition = oldPosition;
        NewPosition = newPosition;
        ReorderedBy = reorderedBy;
    }
}
