using Notrelix.Domain.Common;

namespace Notrelix.Domain.WorkManagement.Forms.Events;

public record FormCreatedDomainEvent : DomainEvent
{
    public Guid FormId { get; }
    public Guid BoardId { get; }
    public string Name { get; }

    public FormCreatedDomainEvent(
        Guid workspaceId,
        Guid formId,
        Guid boardId,
        string name,
        Guid? actorUserId,
        DateTimeOffset occurredAt) 
        : base(occurredAt, workspaceId, actorUserId)
    {
        FormId = formId;
        BoardId = boardId;
        Name = name;
    }
}
