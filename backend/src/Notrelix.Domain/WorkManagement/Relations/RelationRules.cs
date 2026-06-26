namespace Notrelix.Domain.WorkManagement.Relations;

public static class RelationRules
{
    public static void EnsureDifferentBoards(Guid sourceId, Guid targetId)
    {
        if (sourceId == targetId)
            throw new DomainException("Source and target boards must be different for a relation.");
    }

    public static void EnsureBoardsInSameWorkspace(Guid workspaceId, Guid sourceBoardId, Guid targetBoardId, Func<Guid, Guid> getBoardWorkspaceId)
    {
        var sourceWorkspaceId = getBoardWorkspaceId(sourceBoardId);
        if (sourceWorkspaceId != workspaceId)
            throw new BusinessRuleException($"Source board {sourceBoardId} does not belong to workspace {workspaceId}.");

        var targetWorkspaceId = getBoardWorkspaceId(targetBoardId);
        if (targetWorkspaceId != workspaceId)
            throw new BusinessRuleException($"Target board {targetBoardId} does not belong to workspace {workspaceId}.");
    }
}
