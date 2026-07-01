namespace Notrelix.Domain.WorkManagement.Workload;

public class WorkloadAllocation : Entity, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid? BoardId { get; private set; }
    public Guid? ItemId { get; private set; }
    public Guid UserId { get; private set; }
    public DateTime AllocationDate { get; private set; }
    public int AllocatedMinutes { get; private set; }
    public long Version { get; private set; } = 1;

    private WorkloadAllocation() : base() { }

    public static WorkloadAllocation Create(Guid accountId, Guid workspaceId, Guid userId, DateTime date, int minutes)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(userId);
        Guard.NotEmpty(accountId);

        return new WorkloadAllocation
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            UserId = userId,
            AllocationDate = date.Date,
            AllocatedMinutes = minutes
        };
    }
}
