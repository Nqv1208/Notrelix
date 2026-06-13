using Notrelix.Domain.Common;

namespace Notrelix.Domain.WorkManagement.Relations;

public record MirrorValueRefreshedDomainEvent : DomainEvent
{
    public Guid RelationId { get; }
    public Guid ConnectionId { get; }
    public Guid SourceFieldId { get; }
    public string? NewValueJson { get; }

    public MirrorValueRefreshedDomainEvent(
        Guid workspaceId,
        Guid relationId,
        Guid connectionId,
        Guid sourceFieldId,
        string? newValueJson,
        Guid? actorUserId,
        DateTimeOffset occurredAt) 
        : base(occurredAt, workspaceId, actorUserId)
    {
        RelationId = relationId;
        ConnectionId = connectionId;
        SourceFieldId = sourceFieldId;
        NewValueJson = newValueJson;
    }
}
