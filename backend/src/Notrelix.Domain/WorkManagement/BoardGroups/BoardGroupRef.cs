namespace Notrelix.Domain.WorkManagement.BoardGroups;

public sealed record BoardGroupRef(Guid AccountId, Guid WorkspaceId, Guid BoardId, Guid GroupId) : IWorkspaceScoped
{
    public static BoardGroupRef From(BoardGroup group) =>
        new(group.AccountId, group.WorkspaceId, group.BoardId, group.Id);
}
