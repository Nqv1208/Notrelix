namespace Notrelix.Domain.WorkManagement.BoardGroups;

public sealed record BoardGroupRef(Guid WorkspaceId, Guid BoardId, Guid GroupId)
{
    public static BoardGroupRef From(BoardGroup group) =>
        new(group.WorkspaceId, group.BoardId, group.Id);
}
