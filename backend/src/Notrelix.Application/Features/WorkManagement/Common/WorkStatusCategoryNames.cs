namespace Notrelix.Application.Features.WorkManagement.Common;

/// <summary>
/// Public names of <see cref="WorkStatusCategory"/>: "todo", "in_progress",
/// "review", "done". Parsing is strict (exact, case-insensitive names only).
/// </summary>
public static class WorkStatusCategoryNames
{
    public const string Todo = "todo";
    public const string InProgress = "in_progress";
    public const string Review = "review";
    public const string Done = "done";

    public static IReadOnlyList<string> All { get; } = [Todo, InProgress, Review, Done];

    public static string ToName(WorkStatusCategory category) => category switch
    {
        WorkStatusCategory.Todo => Todo,
        WorkStatusCategory.InProgress => InProgress,
        WorkStatusCategory.Review => Review,
        WorkStatusCategory.Done => Done,
        _ => throw new ArgumentOutOfRangeException(nameof(category), category, "Unknown work-status category."),
    };

    public static string? ToName(WorkStatusCategory? category) => category is { } value ? ToName(value) : null;

    public static bool TryParse(string? value, out WorkStatusCategory category)
    {
        category = default;
        switch (value?.Trim().ToLowerInvariant())
        {
            case Todo: category = WorkStatusCategory.Todo; return true;
            case InProgress: category = WorkStatusCategory.InProgress; return true;
            case Review: category = WorkStatusCategory.Review; return true;
            case Done: category = WorkStatusCategory.Done; return true;
            default: return false;
        }
    }

    public static bool IsValidOrNull(string? value) => value is null || TryParse(value, out _);

    /// <summary>Parses an optional public name; null stays null. Call after validation.</summary>
    public static WorkStatusCategory? ParseOptional(string? value) =>
        value is null ? null
        : TryParse(value, out var category) ? category
        : throw new ArgumentException($"Unknown work-status category '{value}'.", nameof(value));
}
