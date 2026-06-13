using Notrelix.Domain.Common;

namespace Notrelix.Domain.WorkManagement.Forms;

public record FormClosedDomainEvent : DomainEvent
{
    public Guid FormId { get; }

    public FormClosedDomainEvent(
        Guid workspaceId,
        Guid formId,
        Guid? actorUserId,
        DateTimeOffset occurredAt)
        : base(occurredAt, workspaceId, actorUserId)
    {
        FormId = formId;
    }
}
