namespace Notrelix.Domain.WorkManagement.Forms.Events;

public record FormClosedDomainEvent : WorkspaceScopedDomainEvent
{
    public Guid FormId { get; }

    public FormClosedDomainEvent(
        Guid accountId,
        Guid workspaceId,
        Guid formId,
        Guid? actorUserId,
        DateTimeOffset occurredAt)
        : base(workspaceId, occurredAt, actorUserId)
    {
        FormId = formId;
    }
}
