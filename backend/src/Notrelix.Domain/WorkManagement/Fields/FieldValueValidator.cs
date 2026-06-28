using System.Text.Json;

namespace Notrelix.Domain.WorkManagement.Fields;

public static class FieldValueValidator
{
    public static void Validate(FieldValue value, FieldType type, FieldSettings settings)
    {
        if (value == null)
            throw new BusinessRuleException("Field value cannot be null.");

        var data = value.Data;
        if (data == null)
            return;

        if (data.Value == "null")
            return;

        JsonDocument? settingsDoc = null;
        try
        {
            if (settings?.Data?.Value != null && settings.Data.Value != "{}")
                settingsDoc = JsonDocument.Parse(settings.Data.Value);
        }
        catch { /* ignore invalid settings JSON */ }

        try
        {
            using var doc = JsonDocument.Parse(data.Value);
            var element = doc.RootElement;
            var kind = element.ValueKind;

            if (kind == JsonValueKind.Null)
                return;

            switch (type)
            {
                case FieldType.Text:
                case FieldType.LongText:
                case FieldType.Link:
                    if (kind != JsonValueKind.String)
                        throw new BusinessRuleException($"Value for field type {type} must be a string.");
                    if (settingsDoc != null && settingsDoc.RootElement.TryGetProperty("maxLength", out var maxLenToken) && maxLenToken.TryGetInt32(out var maxLen))
                    {
                        var strVal = element.GetString() ?? string.Empty;
                        if (strVal.Length > maxLen)
                            throw new BusinessRuleException($"Text value exceeds maximum length of {maxLen} characters.");
                    }
                    break;

                case FieldType.Number:
                    if (kind != JsonValueKind.Number)
                        throw new BusinessRuleException("Value for field type Number must be a number.");
                    var numVal = element.GetDouble();
                    if (settingsDoc != null)
                    {
                        if (settingsDoc.RootElement.TryGetProperty("min", out var minToken) && minToken.TryGetDouble(out var minVal))
                        {
                            if (numVal < minVal)
                                throw new BusinessRuleException($"Number value must be at least {minVal}.");
                        }
                        if (settingsDoc.RootElement.TryGetProperty("max", out var maxToken) && maxToken.TryGetDouble(out var maxVal))
                        {
                            if (numVal > maxVal)
                                throw new BusinessRuleException($"Number value must be at most {maxVal}.");
                        }
                    }
                    break;

                case FieldType.Checkbox:
                    if (kind != JsonValueKind.True && kind != JsonValueKind.False)
                        throw new BusinessRuleException("Value for field type Checkbox must be a boolean.");
                    break;

                case FieldType.Status:
                case FieldType.Select:
                case FieldType.Person:
                    if (kind != JsonValueKind.String)
                        throw new BusinessRuleException($"Value for field type {type} must be a string representing an option ID or user ID.");
                    break;

                case FieldType.MultiSelect:
                    if (kind != JsonValueKind.Array)
                        throw new BusinessRuleException("Value for field type MultiSelect must be an array of option IDs.");
                    break;

                case FieldType.Date:
                    if (kind != JsonValueKind.String)
                        throw new BusinessRuleException("Value for field type Date must be a string representation of DateTimeOffset.");
                    break;

                default:
                    // Rollup, Formula logic can be custom or skip validation here
                    break;
            }
        }
        catch (JsonException)
        {
            throw new BusinessRuleException("Invalid JSON format in field value.");
        }
    }
}
