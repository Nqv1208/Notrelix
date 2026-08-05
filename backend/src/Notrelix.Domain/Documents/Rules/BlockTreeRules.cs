using Notrelix.Domain.Documents.Blocks;

namespace Notrelix.Domain.Documents.Rules;

public static class BlockTreeRules
{
    public static void EnsureNoCycle(Guid blockId, BlockAncestorPath ancestorPath)
    {
        if (blockId == ancestorPath.TargetParentId)
            throw new BusinessRuleException(DocumentRuleCodes.Documents_BlockTree_CannotBeOwnParent, "A block cannot be its own parent.");

        foreach (var ancestorId in ancestorPath.AncestorIds)
        {
            if (ancestorId == blockId)
                throw new BusinessRuleException(DocumentRuleCodes.Documents_BlockTree_MoveWouldCreateCycle, "Block move would create a cycle.");
        }
    }

    public static void EnsureParentSameScope(Guid? parentBlockId, Guid pageId, Guid workspaceId, Func<Guid, (Guid PageId, Guid WorkspaceId)?> getBlockScope)
    {
        if (parentBlockId is null) return;

        var scope = getBlockScope(parentBlockId.Value);
        if (scope is null)
            throw new BusinessRuleException(DocumentRuleCodes.Documents_BlockTree_ParentNotFound, "Parent block not found.");

        if (scope.Value.PageId != pageId || scope.Value.WorkspaceId != workspaceId)
            throw new BusinessRuleException(DocumentRuleCodes.Documents_BlockTree_ParentMustBeInSamePage, "Parent block must belong to the same page and workspace.");
    }
}
