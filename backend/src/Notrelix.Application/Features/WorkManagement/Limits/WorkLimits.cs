using Notrelix.Application.Features.WorkManagement.Abstractions;
using AppConflictException = Notrelix.Application.Common.Exceptions.ConflictException;

namespace Notrelix.Application.Features.WorkManagement.Limits;

/// <summary>The published limits of one board (D78.8), as the limits endpoint answers them.</summary>
public sealed record WorkLimitsDto(int ActiveItemsPerBoard, int FieldsPerBoard, int GroupsPerBoard, int ViewsPerBoard, int ItemsPerBulkEdit);

/// <summary>
/// The published limits of a board (D78.8, UC-WM-T03): 25,000 active items, 200 fields, 50 groups, 50 views and
/// 200 items per bulk edit. A write that would go over one is refused whole with a conflict naming the limit
/// (<c>limit</c>, <c>current</c>, <c>max</c>); nothing is truncated and nothing is partly written.
/// </summary>
public static class WorkLimits
{
    public const int ActiveItemsPerBoard = 25_000;
    public const int FieldsPerBoard = 200;
    public const int GroupsPerBoard = 50;
    public const int ViewsPerBoard = 50;
    public const int ItemsPerBulkEdit = 200;

    public const string ItemsCode = "work-management.limit.items-per-board";
    public const string FieldsCode = "work-management.limit.fields-per-board";
    public const string GroupsCode = "work-management.limit.groups-per-board";
    public const string ViewsCode = "work-management.limit.views-per-board";
    public const string BulkItemsCode = "work-management.limit.items-per-bulk-edit";

    public static readonly WorkLimitsDto Published = new(ActiveItemsPerBoard, FieldsPerBoard, GroupsPerBoard, ViewsPerBoard, ItemsPerBulkEdit);

    /// <summary>
    /// Room for <paramref name="adding"/> more active (not deleted, not archived) items, sub-items included. Run it
    /// after the board's item keys are reserved: that row lock serializes creates on the board, so two creates cannot
    /// both take the last place.
    /// </summary>
    public static async Task EnsureItemRoomAsync(IWorkManagementDbContext context, Guid boardId, int adding, CancellationToken ct)
    {
        if (adding <= 0) return;
        var current = await context.BoardItems.CountAsync(i => i.BoardId == boardId && i.DeletedAt == null && !i.IsArchived, ct);
        Ensure(ItemsCode, "items", current, adding, ActiveItemsPerBoard, "active items");
    }

    /// <summary>Room for <paramref name="adding"/> more fields (deleted ones in the trash do not count). Run it under the field ordering lock.</summary>
    public static async Task EnsureFieldRoomAsync(IWorkManagementDbContext context, Guid boardId, int adding, CancellationToken ct)
    {
        if (adding <= 0) return;
        var current = await context.BoardFields.CountAsync(f => f.BoardId == boardId && f.DeletedAt == null, ct);
        Ensure(FieldsCode, "fields", current, adding, FieldsPerBoard, "fields");
    }

    /// <summary>Room for <paramref name="adding"/> more groups (archived ones count, deleted ones do not). Run it under the group ordering lock.</summary>
    public static async Task EnsureGroupRoomAsync(IWorkManagementDbContext context, Guid boardId, int adding, CancellationToken ct)
    {
        if (adding <= 0) return;
        var current = await context.BoardGroups.CountAsync(g => g.BoardId == boardId && g.DeletedAt == null, ct);
        Ensure(GroupsCode, "groups", current, adding, GroupsPerBoard, "groups");
    }

    /// <summary>Room for <paramref name="adding"/> more views (archived ones count, deleted ones do not).</summary>
    public static async Task EnsureViewRoomAsync(IWorkManagementDbContext context, Guid boardId, int adding, CancellationToken ct)
    {
        if (adding <= 0) return;
        var current = await context.BoardViews.CountAsync(v => v.BoardId == boardId && v.DeletedAt == null, ct);
        Ensure(ViewsCode, "views", current, adding, ViewsPerBoard, "views");
    }

    /// <summary>A bulk edit names at most <see cref="ItemsPerBulkEdit"/> items.</summary>
    public static void EnsureBulkSize(int count)
    {
        if (count <= ItemsPerBulkEdit) return;
        throw Reached(BulkItemsCode, "bulkItems", count, ItemsPerBulkEdit,
            $"A bulk edit changes at most {ItemsPerBulkEdit:N0} items at once; this one names {count:N0}. Select fewer items.");
    }

    private static void Ensure(string code, string limit, int current, int adding, int max, string what)
    {
        if (current + adding <= max) return;
        throw Reached(code, limit, current, max,
            $"A board holds at most {max:N0} {what}; it has {current:N0} and this would add {adding:N0}. Archive or delete some first.");
    }

    private static AppConflictException Reached(string code, string limit, int current, int max, string message) =>
        new(message, code)
        {
            Extensions = new Dictionary<string, object?> { ["limit"] = limit, ["current"] = current, ["max"] = max },
        };
}
