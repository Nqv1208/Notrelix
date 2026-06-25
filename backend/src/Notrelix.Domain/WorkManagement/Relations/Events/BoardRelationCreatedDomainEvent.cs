namespace Notrelix.Domain.WorkManagement.Relations.Events;

public record BoardRelationCreatedDomainEvent : DomainEvent
{
    public Guid RelationId { get; }
    public Guid SourceBoardId { get; }
    public Guid TargetBoardId { get; }

    public BoardRelationCreatedDomainEvent(
        Guid workspaceId,
        Guid relationId,
        Guid sourceBoardId,
        Guid targetBoardId,
        Guid? actorUserId,
        DateTimeOffset occurredAt)
        : base(occurredAt, workspaceId, actorUserId)
    {
        RelationId = relationId;
        SourceBoardId = sourceBoardId;
        TargetBoardId = targetBoardId;
    }
}
