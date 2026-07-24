namespace Notrelix.Domain.WorkManagement.Forms.Events;

[EventName("work-management.form-closed")]
public sealed record FormClosedDomainEvent : WorkspaceScopedDomainEvent
{
    public Guid FormId { get; }

    public FormClosedDomainEvent(
        Guid accountId,
        Guid workspaceId,
        Guid formId,
        DateTimeOffset occurredAt)
        : base(accountId, workspaceId, occurredAt)
    {
        FormId = formId;
    }
}
