namespace Notrelix.Domain.WorkManagement.Fields;

/// <summary>One Status option a new board's Status field starts with.</summary>
public sealed record DefaultWorkStatus(string Name, string Color, WorkStatusCategory Category);

/// <summary>
/// The Status options a new board's Status field is seeded with, in display order.
/// Colours are the work-status token hues (todo grey, in-progress blue, review amber,
/// done green). Creation input only: later edits never re-apply these values.
/// Names follow the creator's profile language, which Application passes in as a creation fact.
/// </summary>
public static class DefaultWorkStatuses
{
    private static readonly IReadOnlyList<DefaultWorkStatus> Vietnamese =
    [
        new("Cần làm", "#7A8391", WorkStatusCategory.Todo),
        new("Đang làm", "#1E7BE0", WorkStatusCategory.InProgress),
        new("Chờ duyệt", "#C18A12", WorkStatusCategory.Review),
        new("Hoàn thành", "#2E8B57", WorkStatusCategory.Done),
    ];

    private static readonly IReadOnlyList<DefaultWorkStatus> English =
    [
        new("To do", "#7A8391", WorkStatusCategory.Todo),
        new("In progress", "#1E7BE0", WorkStatusCategory.InProgress),
        new("In review", "#C18A12", WorkStatusCategory.Review),
        new("Done", "#2E8B57", WorkStatusCategory.Done),
    ];

    /// <summary>English names for "en" / "en-US" style locales; Vietnamese for everything else, including unset.</summary>
    public static IReadOnlyList<DefaultWorkStatus> For(string? locale) =>
        locale is not null && (locale.Equals("en", StringComparison.OrdinalIgnoreCase)
            || locale.StartsWith("en-", StringComparison.OrdinalIgnoreCase)
            || locale.StartsWith("en_", StringComparison.OrdinalIgnoreCase))
            ? English
            : Vietnamese;
}
