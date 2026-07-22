namespace Notrelix.Domain.WorkManagement.Relations.Events;

public record BoardRelationCreatedDomainEvent : WorkspaceScopedDomainEvent
{
    public Guid RelationId { get; }
    public Guid SourceBoardId { get; }
    public Guid TargetBoardId { get; }

    public BoardRelationCreatedDomainEvent(
        Guid accountId,
        Guid workspaceId,
        Guid relationId,
        Guid sourceBoardId,
        Guid targetBoardId,
        DateTimeOffset occurredAt)
        : base(accountId, workspaceId, occurredAt)
    {
        RelationId = relationId;
        SourceBoardId = sourceBoardId;
        TargetBoardId = targetBoardId;
    }
}
