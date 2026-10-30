using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.BoardViews;

/// <summary>
/// A board keeps exactly one default view. When the default view leaves the board's
/// list (delete or archive), the shared active view the person named takes over;
/// when none is named, the oldest shared active view does. A board left with no
/// other shared view has no default until one is set.
/// </summary>
internal static class BoardViewDefaults
{
    public static async Task HandOverAsync(
        IWorkManagementDbContext context,
        BoardView leaving,
        Guid? preferredViewId,
        Guid userId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (!leaving.IsDefault) return;

        var candidates = context.BoardViews.Where(view =>
            view.BoardId == leaving.BoardId
            && view.Id != leaving.Id
            && view.DeletedAt == null
            && !view.IsArchived
            && view.Visibility == BoardViewVisibility.Board);

        BoardView? next = null;
        if (preferredViewId is { } preferred)
        {
            next = await candidates.FirstOrDefaultAsync(view => view.Id == preferred, cancellationToken)
                ?? throw new NotFoundException(nameof(BoardView), preferred);
        }

        next ??= await candidates
            .OrderBy(view => view.CreatedAt)
            .ThenBy(view => view.Id)
            .FirstOrDefaultAsync(cancellationToken);

        leaving.ClearDefault(userId, now);
        next?.SetDefault(userId, now);
    }
}
