using Notrelix.Domain.Common;
using Notrelix.Domain.Enums;

namespace Notrelix.Domain.Events.Board;

public class BoardViewCreatedEvent : BaseEvent
{
    public Guid BoardViewId { get; }
    public Guid BoardId { get; }
    public Guid CreatedBy { get; }
    public string Name { get; }
    public ViewMode ViewMode { get; }

    public BoardViewCreatedEvent(Guid boardViewId, Guid boardId, Guid createdBy, string name, ViewMode viewMode)
    {
        BoardViewId = boardViewId;
        BoardId = boardId;
        CreatedBy = createdBy;
        Name = name;
        ViewMode = viewMode;
    }
}
