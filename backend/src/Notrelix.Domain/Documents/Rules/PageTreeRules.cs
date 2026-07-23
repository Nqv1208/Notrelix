namespace Notrelix.Domain.Documents.Rules;

public static class PageTreeRules
{
    public static void EnsureNoCycle(Guid pageId, Guid targetParentId, Func<Guid, Guid?> getParentId)
    {
        if (pageId == targetParentId)
            throw new BusinessRuleException(BusinessRuleCodes.Documents_PageTree_CannotBeOwnParent, "A page cannot be its own parent.");

        var current = targetParentId;
        while (true)
        {
            var parentId = getParentId(current);
            if (parentId == null) break;
            if (parentId == pageId)
                throw new BusinessRuleException(BusinessRuleCodes.Documents_PageTree_MoveWouldCreateCycle, "Page move would create a cycle in the page tree.");
            current = parentId.Value;
        }
    }
}
