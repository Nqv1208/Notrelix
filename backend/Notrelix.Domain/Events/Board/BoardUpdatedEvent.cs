using Notrelix.Domain.Common;

namespace Notrelix.Domain.Events.Board;

public class BoardUpdatedEvent : BaseEvent
{
    public Guid BoardId { get; }
    public Guid WorkspaceId { get; }
    public Guid UpdatedBy { get; }
    public string Title { get; }

    public BoardUpdatedEvent(Guid boardId, Guid workspaceId, Guid updatedBy, string title)
    {
        BoardId = boardId;
        WorkspaceId = workspaceId;
        UpdatedBy = updatedBy;
        Title = title;
    }
}
