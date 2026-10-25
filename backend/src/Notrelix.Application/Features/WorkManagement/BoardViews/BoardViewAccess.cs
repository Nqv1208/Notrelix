namespace Notrelix.Application.Features.WorkManagement.BoardViews;

/// <summary>
/// WM-8: a private view is its creator's alone. Another member does not learn that it exists, so a write
/// to it is answered like a view that is not there. A Board view is decided by the permission the command
/// declares (an Editor change).
/// </summary>
public static class BoardViewAccess
{
    public static void EnsureCanEdit(BoardView view, Guid userId)
    {
        if (!view.CanBeEditedBy(userId))
            throw new NotFoundException(nameof(BoardView), view.Id);
    }
}
