using Notrelix.Domain.Common;

namespace Notrelix.Domain.Events.Board;

public class BoardGroupCreatedEvent : BaseEvent
{
    public Guid GroupId { get; }
    public Guid BoardId { get; }
    public string Title { get; }

    public BoardGroupCreatedEvent(Guid groupId, Guid boardId, string title)
    {
        GroupId = groupId;
        BoardId = boardId;
        Title = title;
    }
}
