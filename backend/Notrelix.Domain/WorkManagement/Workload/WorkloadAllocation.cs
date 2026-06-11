using Notrelix.Domain.Common;

namespace Notrelix.Domain.WorkManagement.Workload;

public class WorkloadAllocation : Entity
{
    public Guid WorkspaceId { get; private set; }
    public Guid? BoardId { get; private set; }
    public Guid? ItemId { get; private set; }
    public Guid UserId { get; private set; }
    public DateTime AllocationDate { get; private set; }
    public int AllocatedMinutes { get; private set; }

    private WorkloadAllocation() : base() { }

    public static WorkloadAllocation Create(Guid workspaceId, Guid userId, DateTime date, int minutes)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(userId);

        return new WorkloadAllocation
        {
            WorkspaceId = workspaceId,
            UserId = userId,
            AllocationDate = date.Date,
            AllocatedMinutes = minutes
        };
    }
}
