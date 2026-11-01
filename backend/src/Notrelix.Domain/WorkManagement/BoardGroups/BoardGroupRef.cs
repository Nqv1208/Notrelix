namespace Notrelix.Domain.WorkManagement.BoardGroups;

public sealed record BoardGroupRef(Guid AccountId, Guid WorkspaceId, Guid BoardId, Guid GroupId) : IWorkspaceScoped
{
    /// <summary>The reference an Item is placed through; an archived group cannot receive Items.</summary>
    public static BoardGroupRef From(BoardGroup group)
    {
        group.EnsureCanReceiveItems();
        return new(group.AccountId, group.WorkspaceId, group.BoardId, group.Id);
    }
}
