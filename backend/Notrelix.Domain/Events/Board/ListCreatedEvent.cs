using Notrelix.Domain.Common;

namespace Notrelix.Domain.Events.Board;

public class ListCreatedEvent : BaseEvent
{
    public Guid ListId { get; }
    public Guid BoardId { get; }
    public string Title { get; }

    public ListCreatedEvent(Guid listId, Guid boardId, string title)
    {
        ListId = listId;
        BoardId = boardId;
        Title = title;
    }
}
