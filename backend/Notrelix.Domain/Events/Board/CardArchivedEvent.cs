using Notrelix.Domain.Common;

namespace Notrelix.Domain.Events.Board;

public class CardArchivedEvent : BaseEvent
{
    public Guid CardId { get; }
    public Guid ArchivedBy { get; }

    public CardArchivedEvent(Guid cardId, Guid archivedBy)
    {
        CardId = cardId;
        ArchivedBy = archivedBy;
    }
}
