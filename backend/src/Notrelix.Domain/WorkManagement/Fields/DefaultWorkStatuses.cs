namespace Notrelix.Domain.WorkManagement.Fields;

/// <summary>One Status option a new board's Status field starts with.</summary>
public sealed record DefaultWorkStatus(string Name, string Color, WorkStatusCategory Category);

/// <summary>
/// The Status options a new board's Status field is seeded with, in display order.
/// Colours are the work-status token hues (todo grey, in-progress blue, review amber,
/// done green). Creation input only: later edits never re-apply these values.
/// </summary>
public static class DefaultWorkStatuses
{
    public static IReadOnlyList<DefaultWorkStatus> All { get; } =
    [
        new("Cần làm", "#7A8391", WorkStatusCategory.Todo),
        new("Đang làm", "#1E7BE0", WorkStatusCategory.InProgress),
        new("Chờ duyệt", "#C18A12", WorkStatusCategory.Review),
        new("Hoàn thành", "#2E8B57", WorkStatusCategory.Done),
    ];
}
