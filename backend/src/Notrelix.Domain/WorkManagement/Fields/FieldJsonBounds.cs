namespace Notrelix.Domain.WorkManagement.Fields;

/// <summary>
/// Bounds of the flexible JSON a Board field accepts (SPEC WMREQ146, decision
/// WM-V3-DEC-020). Settings and values are stored and rendered as opaque JSON, so
/// their serialized size and nesting depth are capped before any shape validation.
/// </summary>
public static class FieldJsonBounds
{
    /// <summary>Maximum compact serialized length of field settings (characters).</summary>
    public const int MaxSettingsLength = 64 * 1024;

    /// <summary>Maximum compact serialized length of one field value (characters).</summary>
    public const int MaxValueLength = 64 * 1024;

    /// <summary>Maximum JSON nesting depth of settings and values.</summary>
    public const int MaxDepth = 16;

    public static void EnsureWithin(string json, int maxLength, string ruleCode, string subject)
    {
        if (json.Length > maxLength)
            throw new BusinessRuleException(ruleCode, $"{subject} exceeds {maxLength} characters.");

        try
        {
            using var _ = System.Text.Json.JsonDocument.Parse(json, new System.Text.Json.JsonDocumentOptions { MaxDepth = MaxDepth });
        }
        catch (System.Text.Json.JsonException)
        {
            throw new BusinessRuleException(ruleCode, $"{subject} must be valid JSON nested at most {MaxDepth} levels deep.");
        }
    }
}
