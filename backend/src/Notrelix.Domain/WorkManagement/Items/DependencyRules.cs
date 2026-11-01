namespace Notrelix.Domain.WorkManagement.Items;

public static class DependencyRules
{
    /// <summary>
    /// Validates that adding a dependency from <paramref name="itemId"/> to <paramref name="targetDependencyId"/>
    /// does not introduce a cyclic dependency. Uses pre-loaded snapshot data. The rejection names the closing path
    /// as ordered item ids (with the title when <paramref name="titles"/> has one).
    /// </summary>
    public static void EnsureNoCycle(
        Guid itemId,
        Guid targetDependencyId,
        IReadOnlyDictionary<Guid, ItemDependencySnapshot> dependencyGraph,
        IReadOnlyDictionary<Guid, string>? titles = null)
    {
        Guard.NotEmpty(itemId);
        Guard.NotEmpty(targetDependencyId);

        if (itemId == targetDependencyId)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Dependency_CannotDependOnSelf, "An item cannot depend on itself.");

        // Breadth-first from the target along existing "waits on" edges; reaching the item closes the loop.
        var cameFrom = new Dictionary<Guid, Guid>();
        var queue = new Queue<Guid>();
        queue.Enqueue(targetDependencyId);
        cameFrom[targetDependencyId] = itemId;

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!dependencyGraph.TryGetValue(current, out var snapshot)) continue;

            foreach (var next in snapshot.DependencyIds)
            {
                if (next == itemId)
                {
                    var path = new List<Guid> { itemId };
                    for (var node = current; node != itemId; node = cameFrom[node])
                        path.Insert(1, node);
                    path.Add(itemId);

                    string Name(Guid id) => titles is not null && titles.TryGetValue(id, out var title) ? $"{title} ({id})" : id.ToString();
                    throw new BusinessRuleException(
                        WorkManagementRuleCodes.WorkManagement_Dependency_CannotCreateCycle,
                        $"Adding this dependency would create a cycle: {string.Join(" -> ", path.Select(Name))}.");
                }

                if (cameFrom.TryAdd(next, current))
                    queue.Enqueue(next);
            }
        }
    }
}
