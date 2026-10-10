using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Ordering;

/// <summary>
/// The FieldOptions ordering scope: the options of one field. Read from storage
/// under the lock rather than from a previously loaded aggregate, so a concurrent
/// writer's committed option is always seen.
/// </summary>
internal static class FieldOptionSiblings
{
    public static async Task<IReadOnlyList<OrderedSibling>> LoadAsync(
        IWorkManagementDbContext context, Guid fieldId, CancellationToken cancellationToken) =>
        await context.FieldOptions
            .AsNoTracking()
            .Where(option => option.FieldId == fieldId)
            .Select(option => new OrderedSibling(option.Id, option.Position))
            .ToListAsync(cancellationToken);
}
