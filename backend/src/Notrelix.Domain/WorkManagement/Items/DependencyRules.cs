namespace Notrelix.Domain.WorkManagement.Items;

public static class DependencyRules
{
    /// <summary>
    /// Validates that adding a dependency from <paramref name="itemId"/> to <paramref name="targetDependencyId"/>
    /// does not introduce a cyclic dependency.
    /// </summary>
    public static void EnsureNoCycle(
        Guid itemId,
        Guid targetDependencyId,
        Func<Guid, IEnumerable<Guid>> getDependencies)
    {
        Guard.NotEmpty(itemId);
        Guard.NotEmpty(targetDependencyId);
        Guard.NotNull(getDependencies);

        if (itemId == targetDependencyId)
        {
            throw new BusinessRuleException("An item cannot depend on itself.");
        }

        var visited = new HashSet<Guid>();
        var stack = new HashSet<Guid> { itemId };

        bool HasCycle(Guid current)
        {
            if (stack.Contains(current)) return true;
            if (visited.Contains(current)) return false;

            visited.Add(current);
            stack.Add(current);

            foreach (var child in getDependencies(current))
            {
                if (HasCycle(child)) return true;
            }

            stack.Remove(current);
            return false;
        }

        if (HasCycle(targetDependencyId))
        {
            throw new BusinessRuleException("Adding this dependency would create a cycle.");
        }
    }
}
