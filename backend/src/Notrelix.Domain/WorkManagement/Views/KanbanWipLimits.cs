using System.Text.Json;

namespace Notrelix.Domain.WorkManagement.Views;

/// <summary>
/// Optional work-in-progress limit per Kanban lane, kept in the shared view
/// configuration under <c>wipLimits</c>: lane value id (a Status option id, or
/// an option id of the Select field the board is grouped by) to a whole number
/// in <see cref="MinLimit"/>..<see cref="MaxLimit"/>. A lane missing from the
/// map has no limit. Presentation only: going over a limit is a warning in the
/// view and never blocks a write. Lane ids are not checked against the board;
/// an id that names no lane is simply ignored by the view.
/// </summary>
public sealed class KanbanWipLimits : ValueObject
{
    public const string ConfigKey = "wipLimits";
    public const int MinLimit = 1;
    public const int MaxLimit = 999;
    public const int MaxLaneIdLength = 128;

    private readonly SortedDictionary<string, int> _limits;

    private KanbanWipLimits(SortedDictionary<string, int> limits)
    {
        _limits = limits;
    }

    public static KanbanWipLimits None { get; } = new(new SortedDictionary<string, int>(StringComparer.Ordinal));

    public IReadOnlyDictionary<string, int> Limits => _limits;

    public int? LimitFor(string laneValueId) =>
        _limits.TryGetValue(laneValueId, out var limit) ? limit : null;

    public static KanbanWipLimits Create(IEnumerable<KeyValuePair<string, int>> limits)
    {
        Guard.NotNull(limits);
        var map = new SortedDictionary<string, int>(StringComparer.Ordinal);
        foreach (var (laneValueId, limit) in limits)
        {
            EnsureLaneId(laneValueId);
            EnsureInRange(limit);
            map[laneValueId] = limit;
        }
        return map.Count == 0 ? None : new KanbanWipLimits(map);
    }

    /// <summary>
    /// The limits held by a view configuration. Absent or <c>null</c> means no
    /// limits; anything other than an object of lane id to whole number in
    /// range is rejected.
    /// </summary>
    public static KanbanWipLimits FromConfig(JsonValue config)
    {
        Guard.NotNull(config);
        using var document = JsonDocument.Parse(config.Value);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object
            || !root.TryGetProperty(ConfigKey, out var element)
            || element.ValueKind == JsonValueKind.Null)
            return None;

        if (element.ValueKind != JsonValueKind.Object)
            throw Invalid("Kanban WIP limits must be an object of lane id to limit.");

        var limits = new List<KeyValuePair<string, int>>();
        foreach (var property in element.EnumerateObject())
        {
            if (property.Value.ValueKind != JsonValueKind.Number
                || !property.Value.TryGetDecimal(out var number)
                || number != decimal.Truncate(number))
                throw Invalid("A Kanban WIP limit must be a whole number.");
            if (number < MinLimit || number > MaxLimit)
                throw OutOfRange();
            limits.Add(new(property.Name, (int)number));
        }
        return Create(limits);
    }

    private static void EnsureLaneId(string laneValueId)
    {
        if (string.IsNullOrWhiteSpace(laneValueId) || laneValueId.Length > MaxLaneIdLength)
            throw Invalid("A Kanban WIP limit needs a lane id.");
    }

    private static void EnsureInRange(int limit)
    {
        if (limit < MinLimit || limit > MaxLimit)
            throw OutOfRange();
    }

    private static BusinessRuleException Invalid(string message) =>
        new(WorkManagementRuleCodes.WorkManagement_Kanban_WipLimitsInvalid, message);

    private static BusinessRuleException OutOfRange() =>
        new(WorkManagementRuleCodes.WorkManagement_Kanban_WipLimitOutOfRange,
            $"A Kanban WIP limit must be between {MinLimit} and {MaxLimit}.");

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        foreach (var (laneValueId, limit) in _limits)
        {
            yield return laneValueId;
            yield return limit;
        }
    }
}
