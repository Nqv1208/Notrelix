using Notrelix.Domain.Common;

namespace Notrelix.Domain.WorkManagement.Workload;

public record WorkloadAllocatedDomainEvent : DomainEvent
{
    public Guid AllocationId { get; }
    public Guid UserId { get; }
    public int AllocatedMinutes { get; }

    public WorkloadAllocatedDomainEvent(
        Guid workspaceId,
        Guid allocationId,
        Guid userId,
        int allocatedMinutes,
        Guid? actorUserId,
        DateTimeOffset occurredAt) 
        : base(occurredAt, workspaceId, actorUserId)
    {
        AllocationId = allocationId;
        UserId = userId;
        AllocatedMinutes = allocatedMinutes;
    }
}
