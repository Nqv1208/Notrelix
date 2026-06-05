using Notrelix.Domain.Common;

namespace Notrelix.Domain.Events.Board;

public class CardDeletedEvent : BaseEvent
{
    public Guid CardId { get; }
    public Guid DeletedBy { get; }

    public CardDeletedEvent(Guid cardId, Guid deletedBy)
    {
        CardId = cardId;
        DeletedBy = deletedBy;
    }
}
