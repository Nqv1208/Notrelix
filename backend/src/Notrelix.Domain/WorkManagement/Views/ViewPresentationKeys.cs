using System.Text.Json;

namespace Notrelix.Domain.WorkManagement.Views;

/// <summary>
/// Shape of the presentation keys the Calendar and Timeline views keep in the
/// shared view configuration: <c>calendarDateField</c> (the Date field the
/// Calendar places items by: <c>span</c>, <c>due</c> or a field id),
/// <c>timelineGroupBy</c> (<c>group</c>, <c>status</c>, <c>assignee</c> or
/// <c>none</c>) and <c>timelineBarSpan</c> (<c>start-due</c> or
/// <c>created-due</c>). Absent or null means the default. A field id is not
/// checked against the board: a view ignores an id that names no Date field.
/// </summary>
public static class ViewPresentationKeys
{
    public const string CalendarDateField = "calendarDateField";
    public const string TimelineGroupBy = "timelineGroupBy";
    public const string TimelineBarSpan = "timelineBarSpan";
    public const int MaxFieldIdLength = 128;

    private static readonly string[] GroupByValues = ["group", "status", "assignee", "none"];
    private static readonly string[] BarSpanValues = ["start-due", "created-due"];

    public static void Validate(JsonValue config)
    {
        using var document = JsonDocument.Parse(config.Value);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object) return;
        if (Text(root, CalendarDateField) is { } field
            && (string.IsNullOrWhiteSpace(field) || field.Length > MaxFieldIdLength))
            throw Invalid("The Calendar date field must name a field.");
        if (Text(root, TimelineGroupBy) is { } groupBy && !GroupByValues.Contains(groupBy))
            throw Invalid("The Timeline grouping is not one of group, status, assignee or none.");
        if (Text(root, TimelineBarSpan) is { } span && !BarSpanValues.Contains(span))
            throw Invalid("The Timeline bar span is not one of start-due or created-due.");
    }

    private static string? Text(JsonElement root, string key)
    {
        if (!root.TryGetProperty(key, out var element) || element.ValueKind == JsonValueKind.Null)
            return null;
        return element.ValueKind == JsonValueKind.String
            ? element.GetString()
            : throw Invalid($"{key} must be text.");
    }

    private static BusinessRuleException Invalid(string message) =>
        new(WorkManagementRuleCodes.WorkManagement_View_PresentationConfigInvalid, message);
}
