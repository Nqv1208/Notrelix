using Notrelix.Domain.Common;

namespace Notrelix.Domain.Events.Board;

public class CardUpdatedEvent : BaseEvent
{
    public Guid CardId { get; }
    public Guid UpdatedBy { get; }
    public string Title { get; }

    public CardUpdatedEvent(Guid cardId, Guid updatedBy, string title)
    {
        CardId = cardId;
        UpdatedBy = updatedBy;
        Title = title;
    }
}
