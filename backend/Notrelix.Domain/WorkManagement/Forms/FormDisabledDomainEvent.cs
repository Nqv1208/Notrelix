using Notrelix.Domain.Common;

namespace Notrelix.Domain.WorkManagement.Forms;

public record FormDisabledDomainEvent : DomainEvent
{
    public Guid FormId { get; }

    public FormDisabledDomainEvent(
        Guid workspaceId,
        Guid formId,
        Guid? actorUserId,
        DateTimeOffset occurredAt) 
        : base(occurredAt, workspaceId, actorUserId)
    {
        FormId = formId;
    }
}
