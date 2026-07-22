namespace Notrelix.Domain.WorkManagement.Forms.Events;

public record FormCreatedDomainEvent : WorkspaceScopedDomainEvent
{
    public Guid FormId { get; }
    public Guid BoardId { get; }
    public string Name { get; }

    public FormCreatedDomainEvent(
        Guid accountId,
        Guid workspaceId,
        Guid formId,
        Guid boardId,
        string name,
        DateTimeOffset occurredAt)
        : base(accountId, workspaceId, occurredAt)
    {
        FormId = formId;
        BoardId = boardId;
        Name = name;
    }
}
