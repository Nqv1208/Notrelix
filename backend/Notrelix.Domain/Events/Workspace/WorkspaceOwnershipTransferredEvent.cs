using Notrelix.Domain.Common;

namespace Notrelix.Domain.Events.Workspace;

public class WorkspaceOwnershipTransferredEvent : BaseEvent
{
    public Guid WorkspaceId { get; }
    public Guid OldOwnerId { get; }
    public Guid NewOwnerId { get; }
    public Guid TransferredBy { get; }

    public WorkspaceOwnershipTransferredEvent(Guid workspaceId, Guid oldOwnerId, Guid newOwnerId, Guid transferredBy)
    {
        WorkspaceId = workspaceId;
        OldOwnerId = oldOwnerId;
        NewOwnerId = newOwnerId;
        TransferredBy = transferredBy;
    }
}
