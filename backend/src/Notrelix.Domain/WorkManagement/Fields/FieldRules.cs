using System.Text.Json;

namespace Notrelix.Domain.WorkManagement.Fields;

/// <summary>The rules a board owner sets on a field, kept in its settings JSON: <c>required</c> (bool) and <c>guidance</c> (text).</summary>
public static class FieldRules
{
    public const int MaxGuidanceLength = 500;

    public static void Validate(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object) return;
        if (root.TryGetProperty("required", out var required) && required.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_FieldSettings_RequiredMustBeBoolean, "'required' in field settings must be a boolean.");
        if (root.TryGetProperty("guidance", out var guidance)
            && (guidance.ValueKind != JsonValueKind.String || guidance.GetString()!.Length > MaxGuidanceLength))
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_FieldSettings_GuidanceInvalid, $"'guidance' in field settings must be text of at most {MaxGuidanceLength} characters.");
    }

    public static bool IsRequired(string? settings)
    {
        if (string.IsNullOrWhiteSpace(settings) || settings == "{}") return false;
        try
        {
            using var doc = JsonDocument.Parse(settings);
            return doc.RootElement.ValueKind == JsonValueKind.Object
                && doc.RootElement.TryGetProperty("required", out var required) && required.ValueKind == JsonValueKind.True;
        }
        catch (JsonException) { return false; }
    }
}
