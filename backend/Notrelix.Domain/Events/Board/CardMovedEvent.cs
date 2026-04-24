using Notrelix.Domain.Common;

namespace Notrelix.Domain.Events.Board;

public class CardMovedEvent : BaseEvent
{
    public Guid CardId { get; }
    public Guid FromListId { get; }
    public Guid ToListId { get; }
    public double NewPosition { get; }

    public CardMovedEvent(Guid cardId, Guid fromListId, Guid toListId, double newPosition)
    {
        CardId = cardId;
        FromListId = fromListId;
        ToListId = toListId;
        NewPosition = newPosition;
    }
}
