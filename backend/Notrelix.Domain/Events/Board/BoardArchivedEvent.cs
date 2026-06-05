using Notrelix.Domain.Common;

namespace Notrelix.Domain.Events.Board;

public class BoardArchivedEvent : BaseEvent
{
    public Guid BoardId { get; }
    public Guid WorkspaceId { get; }
    public Guid ArchivedBy { get; }

    public BoardArchivedEvent(Guid boardId, Guid workspaceId, Guid archivedBy)
    {
        BoardId = boardId;
        WorkspaceId = workspaceId;
        ArchivedBy = archivedBy;
    }
}
