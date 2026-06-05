using Notrelix.Domain.Common;

namespace Notrelix.Domain.Events.Board;

public class BoardViewDefaultChangedEvent : BaseEvent
{
    public Guid BoardViewId { get; }
    public Guid BoardId { get; }
    public Guid ChangedBy { get; }

    public BoardViewDefaultChangedEvent(Guid boardViewId, Guid boardId, Guid changedBy)
    {
        BoardViewId = boardViewId;
        BoardId = boardId;
        ChangedBy = changedBy;
    }
}
