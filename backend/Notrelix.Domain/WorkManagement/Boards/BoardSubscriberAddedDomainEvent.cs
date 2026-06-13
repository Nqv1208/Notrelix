using Notrelix.Domain.Common;

namespace Notrelix.Domain.WorkManagement.Boards;

public record BoardSubscriberAddedDomainEvent : DomainEvent
{
    public Guid BoardId { get; }
    public Guid UserId { get; }

    public BoardSubscriberAddedDomainEvent(
        Guid workspaceId,
        Guid boardId,
        Guid userId,
        Guid? actorUserId,
        DateTimeOffset occurredAt) 
        : base(occurredAt, workspaceId, actorUserId)
    {
        BoardId = boardId;
        UserId = userId;
    }
}
