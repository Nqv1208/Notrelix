using Notrelix.Domain.Common;

namespace Notrelix.Domain.Events.Board;

public class BoardGroupUpdatedEvent : BaseEvent
{
    public Guid GroupId { get; }
    public Guid BoardId { get; }
    public Guid UpdatedBy { get; }
    public string Title { get; }

    public BoardGroupUpdatedEvent(Guid groupId, Guid boardId, Guid updatedBy, string title)
    {
        GroupId = groupId;
        BoardId = boardId;
        UpdatedBy = updatedBy;
        Title = title;
    }
}
