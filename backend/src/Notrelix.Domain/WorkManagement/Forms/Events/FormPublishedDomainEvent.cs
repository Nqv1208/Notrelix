using Notrelix.Domain.Common;

namespace Notrelix.Domain.WorkManagement.Forms.Events;

public record FormPublishedDomainEvent : DomainEvent
{
    public Guid FormId { get; }

    public FormPublishedDomainEvent(
        Guid workspaceId,
        Guid formId,
        Guid? actorUserId,
        DateTimeOffset occurredAt)
        : base(occurredAt, workspaceId, actorUserId)
    {
        FormId = formId;
    }
}
