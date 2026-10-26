using System.Text.Json;

namespace Notrelix.Application.Features.Automation.Rules.Services;

public static class AutomationTriggerFilter
{
    /// <summary>The group a trigger filter names (<c>{"groupId":"…"}</c>), if any.</summary>
    public static bool TryReadGroup(string? configuration, out Guid groupId)
    {
        groupId = Guid.Empty;
        if (string.IsNullOrWhiteSpace(configuration)) return false;
        try
        {
            using var document = JsonDocument.Parse(configuration);
            return document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("groupId", out var group)
                && group.ValueKind == JsonValueKind.String
                && Guid.TryParse(group.GetString(), out groupId);
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
