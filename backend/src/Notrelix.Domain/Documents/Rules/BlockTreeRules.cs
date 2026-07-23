namespace Notrelix.Domain.Documents.Rules;

public static class BlockTreeRules
{
    public static void EnsureNoCycle(Guid blockId, Guid? targetParentId, Func<Guid, Guid?> getParentId)
    {
        if (targetParentId is null) return;
        if (blockId == targetParentId.Value)
            throw new BusinessRuleException(BusinessRuleCodes.Documents_BlockTree_CannotBeOwnParent, "A block cannot be its own parent.");

        var current = targetParentId.Value;
        while (true)
        {
            var parentId = getParentId(current);
            if (parentId is null) break;
            if (parentId == blockId)
                throw new BusinessRuleException(BusinessRuleCodes.Documents_BlockTree_MoveWouldCreateCycle, "Block move would create a cycle.");
            current = parentId.Value;
        }
    }

    public static void EnsureParentSameScope(Guid? parentBlockId, Guid pageId, Guid workspaceId, Func<Guid, (Guid PageId, Guid WorkspaceId)?> getBlockScope)
    {
        if (parentBlockId is null) return;

        var scope = getBlockScope(parentBlockId.Value);
        if (scope is null)
            throw new BusinessRuleException(BusinessRuleCodes.Documents_BlockTree_ParentNotFound, "Parent block not found.");

        if (scope.Value.PageId != pageId || scope.Value.WorkspaceId != workspaceId)
            throw new BusinessRuleException(BusinessRuleCodes.Documents_BlockTree_ParentMustBeInSamePage, "Parent block must belong to the same page and workspace.");
    }
}
