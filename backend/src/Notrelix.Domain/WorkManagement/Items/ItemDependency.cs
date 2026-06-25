namespace Notrelix.Domain.WorkManagement.Items;

public class ItemDependency : SoftDeletableEntity, IWorkspaceScoped
{
    public Guid WorkspaceId { get; private set; }
    public Guid BoardId { get; private set; }
    public Guid PredecessorItemId { get; private set; }
    public Guid SuccessorItemId { get; private set; }
    public DependencyType DependencyType { get; private set; }
    public int LagMinutes { get; private set; }
    public long Version { get; private set; } = 1;

    private ItemDependency() : base() { }

    public static ItemDependency Create(
        Guid workspaceId,
        Guid boardId,
        Guid predecessorItemId,
        Guid successorItemId,
        DependencyType type,
        int lagMinutes,
        Guid createdBy,
        DateTimeOffset createdAt)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(boardId);
        Guard.NotEmpty(predecessorItemId);
        Guard.NotEmpty(successorItemId);

        if (predecessorItemId == successorItemId)
            throw new BusinessRuleException("An item cannot depend on itself.");

        var dependency = new ItemDependency
        {
            WorkspaceId = workspaceId,
            BoardId = boardId,
            PredecessorItemId = predecessorItemId,
            SuccessorItemId = successorItemId,
            DependencyType = type,
            LagMinutes = lagMinutes
        };

        dependency.SetAuditOnCreate(createdBy, createdAt);
        return dependency;
    }
}
