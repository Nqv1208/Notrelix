using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Ordering;

/// <summary>
/// The BoardGroups ordering scope: the active (not soft-deleted) groups of one
/// board; an archived group is hidden but keeps its key. Passed to <see cref="WorkPlacementResolver"/> so it runs under the lock.
/// </summary>
internal static class BoardGroupSiblings
{
    public static async Task<IReadOnlyList<OrderedSibling>> LoadAsync(
        IWorkManagementDbContext context, Guid boardId, CancellationToken cancellationToken) =>
        await context.BoardGroups
            .AsNoTracking()
            .Where(group => group.BoardId == boardId)
            .Select(group => new OrderedSibling(group.Id, group.Position, group.IsArchived))
            .ToListAsync(cancellationToken);
}
