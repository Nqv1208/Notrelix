namespace Notrelix.Domain.WorkManagement.Workload;

/// <summary>
/// Experimental workload allocation tracking. Capacity rules are not yet
/// implemented, so production code must not depend on this entity. Promotion
/// requires an accepted product/architecture decision under
/// <c>docs/governance/decision-and-exception-policy.md</c> §36; isolation is
/// enforced by <c>ExperimentalRuntimeIsolationTests</c>.
/// </summary>
public class WorkloadAllocation : Entity, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid? BoardId { get; private set; }
    public Guid? ItemId { get; private set; }
    public Guid UserId { get; private set; }
    public DateTimeOffset AllocationDate { get; private set; }
    public int AllocatedMinutes { get; private set; }

    private WorkloadAllocation() : base() { }

    public static WorkloadAllocation Create(Guid accountId, Guid workspaceId, Guid userId, DateTimeOffset date, int minutes)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(userId);
        Guard.NotEmpty(accountId);
        Guard.Positive(minutes);

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
