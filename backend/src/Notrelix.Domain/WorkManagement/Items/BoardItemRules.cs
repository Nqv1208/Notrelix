namespace Notrelix.Domain.WorkManagement.Items;

public static class BoardItemRules
{
    /// <summary>
    /// Validates that assigning <paramref name="targetParentId"/> as parent of <paramref name="itemId"/>
    /// would not create a cycle. Uses pre-loaded snapshot data.
    /// </summary>
    public static void EnsureNoCycle(
        Guid itemId,
        Guid? targetParentId,
        IReadOnlyDictionary<Guid, ItemParentSnapshot> parentChain)
    {
        if (targetParentId is null) return;
        if (itemId == targetParentId.Value)
            throw new BusinessRuleException(BusinessRuleCodes.WorkManagement_Item_CannotBeOwnParent, "An item cannot be its own parent.");

        var current = targetParentId.Value;
        while (true)
        {
            if (!parentChain.TryGetValue(current, out var snapshot))
                break;
            if (snapshot.ParentItemId is null) break;
            if (snapshot.ParentItemId == itemId)
                throw new BusinessRuleException(BusinessRuleCodes.WorkManagement_Item_ParentAssignmentWouldCreateCycle, "Item parent assignment would create a cycle.");
            current = snapshot.ParentItemId.Value;
        }
    }
}
