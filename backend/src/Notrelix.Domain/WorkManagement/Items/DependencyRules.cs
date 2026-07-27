namespace Notrelix.Domain.WorkManagement.Items;

public static class DependencyRules
{
    /// <summary>
    /// Validates that adding a dependency from <paramref name="itemId"/> to <paramref name="targetDependencyId"/>
    /// does not introduce a cyclic dependency. Uses pre-loaded snapshot data.
    /// </summary>
    public static void EnsureNoCycle(
        Guid itemId,
        Guid targetDependencyId,
        IReadOnlyDictionary<Guid, ItemDependencySnapshot> dependencyGraph)
    {
        Guard.NotEmpty(itemId);
        Guard.NotEmpty(targetDependencyId);

        if (itemId == targetDependencyId)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Dependency_CannotDependOnSelf, "An item cannot depend on itself.");

        var visited = new HashSet<Guid>();
        var stack = new HashSet<Guid> { itemId };

        bool HasCycle(Guid current)
        {
            if (stack.Contains(current)) return true;
            if (visited.Contains(current)) return false;

            visited.Add(current);
            stack.Add(current);

            if (dependencyGraph.TryGetValue(current, out var snapshot))
            {
                foreach (var child in snapshot.DependencyIds)
                {
                    if (HasCycle(child)) return true;
                }
            }

            stack.Remove(current);
            return false;
        }

        if (HasCycle(targetDependencyId))
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Dependency_CannotCreateCycle, "Adding this dependency would create a cycle.");
    }
}
