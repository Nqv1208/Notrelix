using System.Text.Json;

namespace Notrelix.Domain.WorkManagement.Fields;

/// <summary>
/// The settings of a Relation field: <c>targetBoardId</c>, the one board whose items the field links to.
/// A Relation value is a set of target item ids; the target board is fixed by the settings, never by the value.
/// </summary>
public static class RelationFieldSettings
{
    /// <summary>The most items one Relation value links to.</summary>
    public const int MaxLinks = 100;

    public const string TargetBoardIdProperty = "targetBoardId";

    /// <summary>The target board id named by the settings, or null when the settings name none.</summary>
    public static Guid? TargetBoardId(string? settingsJson)
    {
        if (string.IsNullOrWhiteSpace(settingsJson) || settingsJson == "{}") return null;
        try
        {
            using var doc = JsonDocument.Parse(settingsJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return null;
            if (!doc.RootElement.TryGetProperty(TargetBoardIdProperty, out var target) || target.ValueKind != JsonValueKind.String) return null;
            return Guid.TryParse(target.GetString(), out var id) && id != Guid.Empty ? id : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
