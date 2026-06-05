using Notrelix.Domain.Common;

namespace Notrelix.Domain.Events.Board;

public class BoardGroupColorChangedEvent : BaseEvent
{
    public Guid GroupId { get; }
    public Guid BoardId { get; }
    public string OldColor { get; }
    public string NewColor { get; }
    public Guid ChangedBy { get; }

    public BoardGroupColorChangedEvent(Guid groupId, Guid boardId, string oldColor, string newColor, Guid changedBy)
    {
        GroupId = groupId;
        BoardId = boardId;
        OldColor = oldColor;
        NewColor = newColor;
        ChangedBy = changedBy;
    }
}
