using System.Text.Json;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.SharedKernel;

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
                    break;

                case FieldType.Number:
                    if (kind != JsonValueKind.Number)
                        throw new BusinessRuleException("Value for field type Number must be a number.");
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
