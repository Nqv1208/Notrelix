using Notrelix.Domain.Common;

namespace Notrelix.Domain.Events.Board;

public class CardLinkedToPageEvent : BaseEvent
{
    public Guid CardId { get; }
    public Guid PageId { get; }

    public CardLinkedToPageEvent(Guid cardId, Guid pageId)
    {
        CardId = cardId;
        PageId = pageId;
    }
}
