using Notrelix.Domain.Common;

namespace Notrelix.Domain.Events.Board;

public class BoardCreatedEvent : BaseEvent
{
    public Guid BoardId { get; }
    public Guid WorkspaceId { get; }
    public Guid CreatedBy { get; }
    public string Title { get; }

    public BoardCreatedEvent(Guid boardId, Guid workspaceId, Guid createdBy, string title)
    {
        BoardId = boardId;
        WorkspaceId = workspaceId;
        CreatedBy = createdBy;
        Title = title;
    }
}
