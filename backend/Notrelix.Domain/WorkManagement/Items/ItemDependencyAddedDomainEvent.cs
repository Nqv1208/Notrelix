using Notrelix.Domain.Common;

namespace Notrelix.Domain.WorkManagement.Items;

public record ItemDependencyAddedDomainEvent : DomainEvent
{
    public Guid BoardId { get; }
    public Guid DependencyId { get; }
    public Guid PredecessorItemId { get; }
    public Guid SuccessorItemId { get; }

    public ItemDependencyAddedDomainEvent(
        Guid workspaceId,
        Guid boardId,
        Guid dependencyId,
        Guid predecessorItemId,
        Guid successorItemId,
        Guid? actorUserId,
        DateTimeOffset occurredAt) 
        : base(occurredAt, workspaceId, actorUserId)
    {
        BoardId = boardId;
        DependencyId = dependencyId;
        PredecessorItemId = predecessorItemId;
        SuccessorItemId = successorItemId;
    }
}
