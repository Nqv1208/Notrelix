using Notrelix.Domain.Common;
using Notrelix.Domain.Enums;

namespace Notrelix.Domain.Events.Board;

public class BoardMemberAddedEvent : BaseEvent
{
    public Guid BoardId { get; }
    public Guid UserId { get; }
    public BoardRole Role { get; }

    public BoardMemberAddedEvent(Guid boardId, Guid userId, BoardRole role)
    {
        BoardId = boardId;
        UserId = userId;
        Role = role;
    }
}
