namespace Notrelix.Domain.WorkManagement.Forms.Events;

public record FormPublishedDomainEvent : WorkspaceScopedDomainEvent
{
    public Guid FormId { get; }

    public FormPublishedDomainEvent(
        Guid workspaceId,
        Guid formId,
        Guid? actorUserId,
        DateTimeOffset occurredAt)
        : base(workspaceId, occurredAt, actorUserId)
    {
        FormId = formId;
    }
}
