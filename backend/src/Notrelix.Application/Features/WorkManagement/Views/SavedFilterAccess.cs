namespace Notrelix.Application.Features.WorkManagement.Views;

/// <summary>
/// A saved filter is a Board child (GOV-033): the Board decides who may see it, and on top of that a
/// personal (Private) filter belongs to its creator alone. Anyone else gets NotFound for it.
/// </summary>
internal static class SavedFilterAccess
{
    public static bool IsVisibleTo(SavedFilter filter, Guid userId) =>
        filter.Visibility == SavedFilterVisibility.Public || filter.CreatedBy == userId;
}
