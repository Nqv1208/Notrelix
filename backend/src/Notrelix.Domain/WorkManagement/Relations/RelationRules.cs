using static Notrelix.Domain.WorkManagement.WorkManagementRuleCodes;

namespace Notrelix.Domain.WorkManagement.Relations;

public static class RelationRules
{
    public static void EnsureDifferentBoards(Guid sourceId, Guid targetId)
    {
        if (sourceId == targetId)
            throw new BusinessRuleException(WorkManagement_Relation_SourceAndTargetMustBeDifferent, "Source and target boards must be different for a relation.");
    }

    public static void EnsureBoardsInSameWorkspace(Guid workspaceId, Guid sourceBoardId, Guid targetBoardId, Func<Guid, Guid> getBoardWorkspaceId)
    {
        var sourceWorkspaceId = getBoardWorkspaceId(sourceBoardId);
        if (sourceWorkspaceId != workspaceId)
            throw new BusinessRuleException(WorkManagement_BoardScopeMismatch, $"Source board {sourceBoardId} does not belong to workspace {workspaceId}.");

        var targetWorkspaceId = getBoardWorkspaceId(targetBoardId);
        if (targetWorkspaceId != workspaceId)
            throw new BusinessRuleException(WorkManagement_BoardScopeMismatch, $"Target board {targetBoardId} does not belong to workspace {workspaceId}.");
    }

    /// <summary>
    /// Ensures no duplicate relation exists between the same source and target boards.
    /// Application supplies existing relations; Domain validates the invariant.
    /// </summary>
    public static void EnsureNoDuplicateRelation(
        Guid sourceBoardId,
        Guid targetBoardId,
        IReadOnlyCollection<BoardRelation> existingRelations)
    {
        Guard.NotNull(existingRelations);

        var duplicate = existingRelations.Any(r =>
            !r.IsDeleted &&
            r.SourceBoardId == sourceBoardId &&
            r.TargetBoardId == targetBoardId);

        if (duplicate)
            throw new BusinessRuleException(
                WorkManagement_Relation_DuplicateNotAllowed,
                $"A relation between boards {sourceBoardId} and {targetBoardId} already exists.");
    }

    /// <summary>
    /// Ensures cardinality limit is not exceeded for a board's relations.
    /// Application supplies existing relations; Domain validates the invariant.
    /// </summary>
    public static void EnsureCardinalityLimit(
        Guid boardId,
        int maxRelationsPerBoard,
        IReadOnlyCollection<BoardRelation> existingRelations)
    {
        Guard.NotNull(existingRelations);
        if (maxRelationsPerBoard <= 0)
            return;

        var count = existingRelations.Count(r =>
            !r.IsDeleted &&
            (r.SourceBoardId == boardId || r.TargetBoardId == boardId));

        if (count >= maxRelationsPerBoard)
            throw new BusinessRuleException(
                WorkManagement_Relation_CardinalityExceeded,
                $"Board {boardId} has reached the maximum of {maxRelationsPerBoard} relations.");
    }
}
