using Notrelix.Domain.Common;

namespace Notrelix.Domain.Events.Board;

public class CardDueDateChangedEvent : BaseEvent
{
    public Guid CardId { get; }
    public DateTime? OldDueDate { get; }
    public DateTime? NewDueDate { get; }
    public Guid ChangedBy { get; }

    public CardDueDateChangedEvent(Guid cardId, DateTime? oldDueDate, DateTime? newDueDate, Guid changedBy)
    {
        CardId = cardId;
        OldDueDate = oldDueDate;
        NewDueDate = newDueDate;
        ChangedBy = changedBy;
    }
}
