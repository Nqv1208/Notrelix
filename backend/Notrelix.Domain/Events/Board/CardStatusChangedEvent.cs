using Notrelix.Domain.Common;
using Notrelix.Domain.Enums;

namespace Notrelix.Domain.Events.Board;

public class CardStatusChangedEvent : BaseEvent
{
    public Guid CardId { get; }
    public CardStatus OldStatus { get; }
    public CardStatus NewStatus { get; }
    public Guid ChangedBy { get; }

    public CardStatusChangedEvent(Guid cardId, CardStatus oldStatus, CardStatus newStatus, Guid changedBy)
    {
        CardId = cardId;
        OldStatus = oldStatus;
        NewStatus = newStatus;
        ChangedBy = changedBy;
    }
}
