namespace Notrelix.Domain.Collaboration.Rules;

public static class CommentRules
{
    public static void EnsureParentSameTarget(ResourceRef target, Guid? parentId, Func<Guid, ResourceRef?> getTarget)
    {
        if (!parentId.HasValue) return;

        var parentTarget = getTarget(parentId.Value);
        if (parentTarget == null)
            throw new BusinessRuleException("Parent comment does not exist.");

        if (parentTarget.ResourceType != target.ResourceType || parentTarget.ResourceId != target.ResourceId)
            throw new BusinessRuleException("Parent comment must belong to the same target resource.");
    }
}
