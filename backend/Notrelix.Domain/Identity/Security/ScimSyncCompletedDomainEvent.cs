using Notrelix.Domain.Common;

namespace Notrelix.Domain.Identity.Security;

public record ScimSyncCompletedDomainEvent : DomainEvent
{
    public Guid SyncId { get; }

    public ScimSyncCompletedDomainEvent(
        Guid workspaceId,
        Guid syncId,
        DateTimeOffset occurredAt) 
        : base(occurredAt, workspaceId, null)
    {
        SyncId = syncId;
    }
}
