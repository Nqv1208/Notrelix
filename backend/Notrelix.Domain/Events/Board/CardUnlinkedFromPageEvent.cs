using Notrelix.Domain.Common;

namespace Notrelix.Domain.Events.Board;

public class CardUnlinkedFromPageEvent : BaseEvent
{
    public Guid CardId { get; }
    public Guid PageId { get; }
    public Guid UnlinkedBy { get; }

    public CardUnlinkedFromPageEvent(Guid cardId, Guid pageId, Guid unlinkedBy)
    {
        CardId = cardId;
        PageId = pageId;
        UnlinkedBy = unlinkedBy;
    }
}
