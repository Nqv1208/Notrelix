using System.Text.Json;

namespace Notrelix.Domain.WorkManagement.Forms;

public sealed class FormQuestionConfig : ValueObject
{
    public bool Required { get; }
    public string? Placeholder { get; }
    public string? HelpText { get; }
    public int? MaxLength { get; }
    public int? MinValue { get; }
    public int? MaxValue { get; }
    public int? MaxFileSizeMb { get; }

    private FormQuestionConfig() { }

    private FormQuestionConfig(
        bool required,
        string? placeholder,
        string? helpText,
        int? maxLength,
        int? minValue,
        int? maxValue,
        int? maxFileSizeMb)
    {
        Required = required;
        Placeholder = placeholder;
        HelpText = helpText;
        MaxLength = maxLength;
        MinValue = minValue;
        MaxValue = maxValue;
        MaxFileSizeMb = maxFileSizeMb;
    }

    public static FormQuestionConfig FromConfig(FormQuestionType type, string? configJson)
    {
        if (string.IsNullOrWhiteSpace(configJson) || configJson == "{}")
            return new FormQuestionConfig(false, null, null, null, null, null, null);

        JsonElement root;
        try
        {
            using var document = JsonDocument.Parse(configJson);
            root = document.RootElement.Clone();
        }
        catch (JsonException ex)
        {
            throw new BusinessRuleException($"Invalid config JSON: {ex.Message}");
        }

        var required = GetBool(root, "required") ?? false;
        var placeholder = GetString(root, "placeholder");
        var helpText = GetString(root, "helpText");
        var maxLength = GetInt(root, "maxLength");
        var minValue = GetInt(root, "minValue");
        var maxValue = GetInt(root, "maxValue");
        var maxFileSizeMb = GetInt(root, "maxFileSizeMb");

        ValidateConfig(type, maxLength, minValue, maxValue, maxFileSizeMb);

        return new FormQuestionConfig(required, placeholder, helpText, maxLength, minValue, maxValue, maxFileSizeMb);
    }

    private static void ValidateConfig(FormQuestionType type, int? maxLength, int? minValue, int? maxValue, int? maxFileSizeMb)
    {
        if (maxLength.HasValue && type != FormQuestionType.ShortText && type != FormQuestionType.LongText)
            throw new BusinessRuleException(BusinessRuleCodes.WorkManagement_FormQuestion_MaxLengthInvalidForType, "MaxLength is not supported for this field type.");

        if ((minValue.HasValue || maxValue.HasValue) && type != FormQuestionType.Number)
            throw new BusinessRuleException(BusinessRuleCodes.WorkManagement_FormQuestion_MinMaxInvalidForType, "MinValue/MaxValue are not supported for this field type.");

        if (maxFileSizeMb.HasValue && type != FormQuestionType.FileUpload)
            throw new BusinessRuleException(BusinessRuleCodes.WorkManagement_FormQuestion_MaxFileSizeInvalidForType, "MaxFileSizeMb is not supported for this field type.");

        if (maxLength.HasValue && maxLength.Value <= 0)
            throw new BusinessRuleException(BusinessRuleCodes.WorkManagement_FormQuestion_MaxLengthMustBePositive, "MaxLength must be positive.");

        if (maxFileSizeMb.HasValue && maxFileSizeMb.Value <= 0)
            throw new BusinessRuleException(BusinessRuleCodes.WorkManagement_FormQuestion_MaxFileSizeMustBePositive, "MaxFileSizeMb must be positive.");

        if (minValue.HasValue && maxValue.HasValue && minValue.Value > maxValue.Value)
            throw new BusinessRuleException(BusinessRuleCodes.WorkManagement_FormQuestion_MinCannotExceedMax, "MinValue cannot be greater than MaxValue.");
    }

    private static bool? GetBool(JsonElement root, string property)
    {
        return root.TryGetProperty(property, out var el) && el.ValueKind == JsonValueKind.True
            ? true
            : el.ValueKind == JsonValueKind.False ? false : null;
    }

    private static string? GetString(JsonElement root, string property)
    {
        return root.TryGetProperty(property, out var el) && el.ValueKind == JsonValueKind.String
            ? el.GetString()
            : null;
    }

    private static int? GetInt(JsonElement root, string property)
    {
        if (root.TryGetProperty(property, out var el) && el.ValueKind == JsonValueKind.Number)
            return el.TryGetInt32(out var val) ? val : null;
        return null;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Required;
        yield return Placeholder;
        yield return HelpText;
        yield return MaxLength;
        yield return MinValue;
        yield return MaxValue;
        yield return MaxFileSizeMb;
    }
}
