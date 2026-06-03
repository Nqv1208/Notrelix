using Notrelix.Domain.Common;
using Notrelix.Domain.Enums;

namespace Notrelix.Domain.Events.Board;

public class CardPriorityChangedEvent : BaseEvent
{
    public Guid CardId { get; }
    public CardPriority? OldPriority { get; }
    public CardPriority? NewPriority { get; }
    public Guid ChangedBy { get; }

    public CardPriorityChangedEvent(Guid cardId, CardPriority? oldPriority, CardPriority? newPriority, Guid changedBy)
    {
        CardId = cardId;
        OldPriority = oldPriority;
        NewPriority = newPriority;
        ChangedBy = changedBy;
    }
}
