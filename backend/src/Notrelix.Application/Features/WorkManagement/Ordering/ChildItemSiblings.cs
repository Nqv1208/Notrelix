using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Ordering;

/// <summary>
/// The ChildItems ordering scope: the active (not soft-deleted) sub-items of one
/// parent item, archived ones included because they keep their place. Passed to
/// <see cref="WorkPlacementResolver"/> so it runs under the scope lock.
/// </summary>
internal static class ChildItemSiblings
{
    public static async Task<IReadOnlyList<OrderedSibling>> LoadAsync(
        IWorkManagementDbContext context, Guid parentItemId, CancellationToken cancellationToken) =>
        await context.BoardItems
            .AsNoTracking()
            .Where(item => item.ParentItemId == parentItemId)
            .Select(item => new OrderedSibling(item.Id, item.Position))
            .ToListAsync(cancellationToken);
}
