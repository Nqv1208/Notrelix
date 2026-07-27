namespace Notrelix.Domain.WorkManagement.Forms.Events;

[EventName("work-management.form-published")]
public sealed record FormPublishedDomainEvent : WorkspaceScopedDomainEvent
{
    public Guid FormId { get; }

    public FormPublishedDomainEvent(
        Guid accountId,
        Guid workspaceId,
        Guid formId,
        DateTimeOffset occurredAt)
        : base(accountId, workspaceId, occurredAt)
    {
        FormId = formId;
    }
}
