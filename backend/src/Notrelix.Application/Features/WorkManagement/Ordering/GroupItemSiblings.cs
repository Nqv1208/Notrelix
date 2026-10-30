using Notrelix.Application.Features.WorkManagement.Abstractions;
namespace Notrelix.Application.Features.WorkManagement.Ordering;

/// <summary>
/// The GroupItems ordering scope (SPEC WM-V3-DEC-016): the active (not soft-deleted)
/// root items of one group. Callers pass this loader to <see cref="WorkPlacementResolver"/>
/// so it runs only after the scope lock is held.
/// </summary>
internal static class GroupItemSiblings
{
    public static async Task<IReadOnlyList<OrderedSibling>> LoadAsync(
        IWorkManagementDbContext context, Guid groupId, CancellationToken cancellationToken) =>
        await context.BoardItems
            .AsNoTracking()
            .Where(item => item.GroupId == groupId && item.ParentItemId == null)
            .Select(item => new OrderedSibling(item.Id, item.Position, item.IsArchived))
            .ToListAsync(cancellationToken);
}
