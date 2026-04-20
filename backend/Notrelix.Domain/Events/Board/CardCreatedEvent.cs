using Notrelix.Domain.Common;

namespace Notrelix.Domain.Events.Board;

public class CardCreatedEvent : BaseEvent
{
    public Guid CardId { get; }
    public Guid BoardId { get; }
    public Guid ListId { get; }
    public string Title { get; }
    public Guid CreatedBy { get; }

    public CardCreatedEvent(Guid cardId, Guid boardId, Guid listId, string title, Guid createdBy)
    {
        CardId = cardId;
        BoardId = boardId;
        ListId = listId;
        Title = title;
        CreatedBy = createdBy;
    }
}
