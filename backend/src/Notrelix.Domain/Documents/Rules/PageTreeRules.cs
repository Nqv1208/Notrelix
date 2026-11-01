namespace Notrelix.Domain.Documents.Rules;

public static class PageTreeRules
{
    /// <summary>Pages nest at most this many levels; a root page is level 1.</summary>
    public const int MaxDepth = 10;

    /// <summary>
    /// A page may be placed under a parent only if it stays within <see cref="MaxDepth"/>. Application supplies
    /// the parent's level (a root page is 1); a page placed with a subtree passes the level of its deepest
    /// descendant counted from the page itself as <paramref name="subtreeDepth"/>.
    /// </summary>
    public static void EnsureCanNestUnder(int parentDepth, int subtreeDepth = 1)
    {
        if (parentDepth + subtreeDepth > MaxDepth)
            throw new BusinessRuleException(DocumentRuleCodes.Documents_PageTree_MaxDepthExceeded, $"Pages nest at most {MaxDepth} levels.");
    }

    public static void EnsureNoCycle(Guid pageId, Guid targetParentId, Func<Guid, Guid?> getParentId)
    {
        if (pageId == targetParentId)
            throw new BusinessRuleException(DocumentRuleCodes.Documents_PageTree_CannotBeOwnParent, "A page cannot be its own parent.");

        var current = targetParentId;
        while (true)
        {
            var parentId = getParentId(current);
            if (parentId == null) break;
            if (parentId == pageId)
                throw new BusinessRuleException(DocumentRuleCodes.Documents_PageTree_MoveWouldCreateCycle, "Page move would create a cycle in the page tree.");
            current = parentId.Value;
        }
    }
}
