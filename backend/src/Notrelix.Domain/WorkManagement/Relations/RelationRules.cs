using Notrelix.Domain.Common.Exceptions;
using static Notrelix.Domain.Common.Exceptions.BusinessRuleCodes;

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
            throw new BusinessRuleException(Common_BoardScopeMismatch, $"Source board {sourceBoardId} does not belong to workspace {workspaceId}.");

        var targetWorkspaceId = getBoardWorkspaceId(targetBoardId);
        if (targetWorkspaceId != workspaceId)
            throw new BusinessRuleException(Common_BoardScopeMismatch, $"Target board {targetBoardId} does not belong to workspace {workspaceId}.");
    }
}
