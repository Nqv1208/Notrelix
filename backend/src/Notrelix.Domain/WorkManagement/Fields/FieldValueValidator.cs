using System.Globalization;
using System.Text.Json;

namespace Notrelix.Domain.WorkManagement.Fields;

public static class FieldValueValidator
{
    private static readonly string[] DateFormats = ["O"];

    public static void Validate(FieldValue value, FieldType type, FieldSettings settings)
    {
        if (value == null)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_FieldValue_CannotBeNull, "Field value cannot be null.");

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
        catch (JsonException)
        {
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_FieldSettings_InvalidJsonFormat, "Field settings contain invalid JSON.");
        }

        JsonDocument? doc = null;
        try
        {
            doc = JsonDocument.Parse(data.Value);
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
                        throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_FieldValue_InvalidStringFormat, $"Value for field type {type} must be a string.");
                    if (settingsDoc != null && settingsDoc.RootElement.TryGetProperty("maxLength", out var maxLenToken) && maxLenToken.TryGetInt32(out var maxLen))
                    {
                        var strVal = element.GetString() ?? string.Empty;
                        if (strVal.Length > maxLen)
                            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_FieldValue_TextExceedsMaxLength, $"Text value exceeds maximum length of {maxLen} characters.");
                    }
                    break;

                case FieldType.Number:
                    if (kind != JsonValueKind.Number)
                        throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_FieldValue_InvalidStringValue, "Value for field type Number must be a number.");
                    var numVal = element.GetDecimal();
                    if (settingsDoc != null)
                    {
                        if (settingsDoc.RootElement.TryGetProperty("min", out var minToken) && minToken.TryGetDecimal(out var minVal))
                        {
                            if (numVal < minVal)
                                throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_FieldValue_NumberBelowMin, $"Number value must be at least {minVal}.");
                        }
                        if (settingsDoc.RootElement.TryGetProperty("max", out var maxToken) && maxToken.TryGetDecimal(out var maxVal))
                        {
                            if (numVal > maxVal)
                                throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_FieldValue_NumberAboveMax, $"Number value must be at most {maxVal}.");
                        }
                    }
                    break;

                case FieldType.Checkbox:
                    if (kind != JsonValueKind.True && kind != JsonValueKind.False)
                        throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_FieldValue_InvalidBooleanValue, "Value for field type Checkbox must be a boolean.");
                    break;

                case FieldType.Status:
                case FieldType.Select:
                case FieldType.Person:
                    if (kind != JsonValueKind.String)
                        throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_FieldValue_InvalidSelectValue, $"Value for field type {type} must be a string representing an option ID or user ID.");
                    var idStr = element.GetString();
                    if (string.IsNullOrWhiteSpace(idStr) || !Guid.TryParse(idStr, out _))
                        throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_FieldValue_InvalidSelectValue, $"Value for field type {type} must be a valid GUID.");
                    break;

                case FieldType.MultiSelect:
                    if (kind != JsonValueKind.Array)
                        throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_FieldValue_InvalidMultiSelectValue, "Value for field type MultiSelect must be an array of option IDs.");
                    var seen = new HashSet<string>();
                    foreach (var item in element.EnumerateArray())
                    {
                        if (item.ValueKind != JsonValueKind.String)
                            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_FieldValue_InvalidMultiSelectValue, "MultiSelect items must be strings.");
                        var itemId = item.GetString();
                        if (string.IsNullOrWhiteSpace(itemId) || !Guid.TryParse(itemId, out _))
                            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_FieldValue_InvalidMultiSelectValue, "MultiSelect items must be valid GUIDs.");
                        if (!seen.Add(itemId))
                            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_FieldValue_InvalidMultiSelectValue, "MultiSelect value contains duplicate option IDs.");
                    }
                    break;

                case FieldType.Date:
                    if (kind != JsonValueKind.String)
                        throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_FieldValue_InvalidDateValue, "Value for field type Date must be a string representation of DateTimeOffset.");
                    var dateStr = element.GetString();
                    if (dateStr is not null && !DateTimeOffset.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                        throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_FieldValue_InvalidDateValue, $"Value '{dateStr}' is not a valid date.");
                    break;

                case FieldType.Formula:
                case FieldType.Rollup:
                    throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_FieldValue_CalculatedFieldCannotBeWritten, $"Cannot write to calculated field type {type}.");

                default:
                    throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_FieldValue_UnknownFieldType, $"Unsupported field type {type}.");
            }
        }
        catch (JsonException)
        {
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_FieldValue_InvalidJsonFormat, "Invalid JSON format in field value.");
        }
        finally
        {
            doc?.Dispose();
            settingsDoc?.Dispose();
        }
    }
}
