using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Ordering;

/// <summary>
/// The BoardFields ordering scope: the active (not soft-deleted) fields of one
/// board. Passed to <see cref="WorkPlacementResolver"/> so it runs under the lock.
/// </summary>
internal static class BoardFieldSiblings
{
    public static async Task<IReadOnlyList<OrderedSibling>> LoadAsync(
        IWorkManagementDbContext context, Guid boardId, CancellationToken cancellationToken) =>
        await context.BoardFields
            .AsNoTracking()
            .Where(field => field.BoardId == boardId)
            .Select(field => new OrderedSibling(field.Id, field.Position))
            .ToListAsync(cancellationToken);
}
