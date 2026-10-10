using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Ordering;

/// <summary>
/// The ChecklistItems ordering scope: the items of one checklist, read from storage
/// under the lock so a concurrent writer's committed item is always seen.
/// </summary>
internal static class ChecklistItemSiblings
{
    public static async Task<IReadOnlyList<OrderedSibling>> LoadAsync(
        IWorkManagementDbContext context, Guid checklistId, CancellationToken cancellationToken) =>
        await context.ChecklistItems
            .AsNoTracking()
            .Where(item => item.ChecklistId == checklistId)
            .Select(item => new OrderedSibling(item.Id, item.Position))
            .ToListAsync(cancellationToken);
}
