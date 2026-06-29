namespace Notrelix.Domain.WorkManagement.Forms.Events;

public record FormCreatedDomainEvent : WorkspaceScopedDomainEvent
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
        : base(workspaceId, occurredAt, actorUserId)
    {
        FormId = formId;
        BoardId = boardId;
        Name = name;
    }
}
