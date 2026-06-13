using Notrelix.Domain.Common;

namespace Notrelix.Domain.WorkManagement.Relations;

public record BoardItemDisconnectedDomainEvent : DomainEvent
{
    public Guid RelationId { get; }
    public Guid ConnectionId { get; }
    public Guid SourceItemId { get; }
    public Guid TargetItemId { get; }

    public BoardItemDisconnectedDomainEvent(
        Guid workspaceId,
        Guid relationId,
        Guid connectionId,
        Guid sourceItemId,
        Guid targetItemId,
        Guid? actorUserId,
        DateTimeOffset occurredAt) 
        : base(occurredAt, workspaceId, actorUserId)
    {
        RelationId = relationId;
        ConnectionId = connectionId;
        SourceItemId = sourceItemId;
        TargetItemId = targetItemId;
    }
}
