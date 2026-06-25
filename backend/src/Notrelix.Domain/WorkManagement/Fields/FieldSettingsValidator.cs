namespace Notrelix.Domain.WorkManagement.Fields;

public static class FieldSettingsValidator
{
    public static void Validate(FieldSettings settings, FieldType type)
    {
        Guard.NotNull(settings);

        var data = settings.Data.Value;

        if (string.IsNullOrWhiteSpace(data) || data == "{}")
            return;

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(data);
            var root = doc.RootElement;

            switch (type)
            {
                case FieldType.Number:
                    if (root.TryGetProperty("min", out var min) && min.ValueKind != System.Text.Json.JsonValueKind.Number)
                        throw new BusinessRuleException("'min' in Number field settings must be a number.");
                    if (root.TryGetProperty("max", out var max) && max.ValueKind != System.Text.Json.JsonValueKind.Number)
                        throw new BusinessRuleException("'max' in Number field settings must be a number.");
                    break;

                case FieldType.Text:
                case FieldType.LongText:
                    if (root.TryGetProperty("maxLength", out var maxLen) && maxLen.ValueKind != System.Text.Json.JsonValueKind.Number)
                        throw new BusinessRuleException("'maxLength' in Text field settings must be a number.");
                    break;

                case FieldType.Date:
                    if (root.TryGetProperty("includeTime", out var incTime) && incTime.ValueKind != System.Text.Json.JsonValueKind.True && incTime.ValueKind != System.Text.Json.JsonValueKind.False)
                        throw new BusinessRuleException("'includeTime' in Date field settings must be a boolean.");
                    break;

                case FieldType.Status:
                    if (!root.TryGetProperty("transitions", out var _))
                        throw new BusinessRuleException("Status field settings must include 'transitions'.");
                    break;
            }
        }
        catch (System.Text.Json.JsonException)
        {
            throw new BusinessRuleException("Invalid JSON format in field settings.");
        }
    }
}
