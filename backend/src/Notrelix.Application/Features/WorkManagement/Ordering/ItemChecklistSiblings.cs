using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Ordering;

/// <summary>
/// The ItemChecklists ordering scope: the active (not soft-deleted) checklists of
/// one board item. Passed to <see cref="WorkPlacementResolver"/> so it runs under the lock.
/// </summary>
internal static class ItemChecklistSiblings
{
    public static async Task<IReadOnlyList<OrderedSibling>> LoadAsync(
        IWorkManagementDbContext context, Guid boardItemId, CancellationToken cancellationToken) =>
        await context.Checklists
            .AsNoTracking()
            .Where(checklist => checklist.ItemId == boardItemId)
            .Select(checklist => new OrderedSibling(checklist.Id, checklist.Position))
            .ToListAsync(cancellationToken);
}
