using Notrelix.Domain.Common;

namespace Notrelix.Domain.Events.Board;

public class CardDueDateSetEvent : BaseEvent
{
    public Guid CardId { get; }
    public DateTime? DueDate { get; }

    public CardDueDateSetEvent(Guid cardId, DateTime? dueDate)
    {
        CardId = cardId;
        DueDate = dueDate;
    }
}
