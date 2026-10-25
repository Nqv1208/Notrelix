namespace Notrelix.Application.Features.Automation.Ports.WorkManagement;

/// <summary>
/// Parses the Automation-owned MoveItem action configuration
/// (<c>{"itemId":"…","targetGroupId":"…"}</c>). Shared by rule authoring (create
/// and enable authority checks) and execution so both read the same target.
/// </summary>
public static class AutomationMoveItemConfiguration
{
    public const string ActionType = "MoveItem";

    /// <summary>
    /// AU-1: the board-scoped form, <c>{"targetGroupId":"…"}</c>; the item is the one the
    /// trigger fact is about. Rules made before that carry a fixed <c>itemId</c> too.
    /// </summary>
    public static bool TryParseTarget(string? configuration, out Guid targetGroupId)
    {
        targetGroupId = Guid.Empty;
        if (string.IsNullOrWhiteSpace(configuration)) return false;
        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(configuration);
            var root = document.RootElement;
            return root.ValueKind == System.Text.Json.JsonValueKind.Object
                && root.TryGetProperty("targetGroupId", out var group)
                && group.ValueKind == System.Text.Json.JsonValueKind.String
                && Guid.TryParse(group.GetString(), out targetGroupId)
                && targetGroupId != Guid.Empty;
        }
        catch (System.Text.Json.JsonException)
        {
            return false;
        }
    }

    public static bool TryParse(string? configuration, out Guid itemId, out Guid targetGroupId)
    {
        itemId = Guid.Empty;
        targetGroupId = Guid.Empty;

        if (string.IsNullOrWhiteSpace(configuration))
        {
            return false;
        }

        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(configuration);
            var root = document.RootElement;

            if (root.ValueKind != System.Text.Json.JsonValueKind.Object
                || !root.TryGetProperty("itemId", out var itemIdElement)
                || !root.TryGetProperty("targetGroupId", out var groupElement)
                || !Guid.TryParse(itemIdElement.GetString(), out itemId)
                || !Guid.TryParse(groupElement.GetString(), out targetGroupId))
            {
                return false;
            }

            return itemId != Guid.Empty && targetGroupId != Guid.Empty;
        }
        catch (System.Text.Json.JsonException)
        {
            return false;
        }
    }
}
