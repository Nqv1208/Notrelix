namespace Notrelix.Domain.WorkManagement.Items;

public static class BoardItemRules
{
    public static void EnsureNoCycle(Guid itemId, Guid? targetParentId, Func<Guid, Guid?> getParentId)
    {
        if (targetParentId is null) return;
        if (itemId == targetParentId.Value)
            throw new BusinessRuleException("An item cannot be its own parent.");

        var current = targetParentId.Value;
        while (true)
        {
            var parentId = getParentId(current);
            if (parentId is null) break;
            if (parentId == itemId)
                throw new BusinessRuleException("Item parent assignment would create a cycle.");
            current = parentId.Value;
        }
    }
}
