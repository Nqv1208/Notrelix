namespace Notrelix.Domain.WorkManagement.Forms.Events;

[EventName("work-management.form-public-access-changed")]
public sealed record FormPublicAccessChangedDomainEvent : WorkspaceScopedDomainEvent
{
    public Guid FormId { get; }
    public Guid BoardId { get; }
    public bool IsPublic { get; }

    public FormPublicAccessChangedDomainEvent(
        Guid accountId,
        Guid workspaceId,
        Guid formId,
        Guid boardId,
        bool isPublic,
        DateTimeOffset occurredAt)
        : base(accountId, workspaceId, occurredAt)
    {
        FormId = formId;
        BoardId = boardId;
        IsPublic = isPublic;
    }
}
