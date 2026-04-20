using Notrelix.Domain.Common;

namespace Notrelix.Domain.Events.Board;

public class CardAssignedEvent : BaseEvent
{
    public Guid CardId { get; }
    public Guid AssignedUserId { get; }
    public Guid AssignedBy { get; }

    public CardAssignedEvent(Guid cardId, Guid assignedUserId, Guid assignedBy)
    {
        CardId = cardId;
        AssignedUserId = assignedUserId;
        AssignedBy = assignedBy;
    }
}
