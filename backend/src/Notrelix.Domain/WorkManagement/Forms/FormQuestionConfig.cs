using System.Text.Json;

namespace Notrelix.Domain.WorkManagement.Forms;

public sealed class FormQuestionConfig : ValueObject
{
    public bool Required { get; private set; }
    public string? Placeholder { get; private set; }
    public string? HelpText { get; private set; }
    public int? MaxLength { get; private set; }
    public int? MinValue { get; private set; }
    public int? MaxValue { get; private set; }
    public int? MaxFileSizeMb { get; private set; }
    public IReadOnlyList<FormQuestionOption> Options { get; private set; } = new List<FormQuestionOption>();
    public const int MaxOptions = 50;

    private FormQuestionConfig() { }

    private FormQuestionConfig(
        bool required,
        string? placeholder,
        string? helpText,
        int? maxLength,
        int? minValue,
        int? maxValue,
        int? maxFileSizeMb,
        IReadOnlyList<FormQuestionOption>? options = null)
    {
        Options = options ?? new List<FormQuestionOption>();
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
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_FormQuestion_InvalidConfigJson, $"Invalid config JSON: {ex.Message}");
        }

        var required = GetBool(root, "required") ?? false;
        var placeholder = GetString(root, "placeholder");
        var helpText = GetString(root, "helpText");
        var maxLength = GetInt(root, "maxLength");
        var minValue = GetInt(root, "minValue");
        var maxValue = GetInt(root, "maxValue");
        var maxFileSizeMb = GetInt(root, "maxFileSizeMb");

        ValidateConfig(type, maxLength, minValue, maxValue, maxFileSizeMb);
        var options = ReadOptions(type, root);

        return new FormQuestionConfig(required, placeholder, helpText, maxLength, minValue, maxValue, maxFileSizeMb, options);
    }

    private static void ValidateConfig(FormQuestionType type, int? maxLength, int? minValue, int? maxValue, int? maxFileSizeMb)
    {
        if (maxLength.HasValue && type != FormQuestionType.ShortText && type != FormQuestionType.LongText)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_FormQuestion_MaxLengthInvalidForType, "MaxLength is not supported for this field type.");

        if ((minValue.HasValue || maxValue.HasValue) && type != FormQuestionType.Number)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_FormQuestion_MinMaxInvalidForType, "MinValue/MaxValue are not supported for this field type.");

        if (maxFileSizeMb.HasValue && type != FormQuestionType.FileUpload)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_FormQuestion_MaxFileSizeInvalidForType, "MaxFileSizeMb is not supported for this field type.");

        if (maxLength.HasValue && maxLength.Value <= 0)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_FormQuestion_MaxLengthMustBePositive, "MaxLength must be positive.");

        if (maxFileSizeMb.HasValue && maxFileSizeMb.Value <= 0)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_FormQuestion_MaxFileSizeMustBePositive, "MaxFileSizeMb must be positive.");

        if (minValue.HasValue && maxValue.HasValue && minValue.Value > maxValue.Value)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_FormQuestion_MinCannotExceedMax, "MinValue cannot be greater than MaxValue.");
    }

    private static List<FormQuestionOption> ReadOptions(FormQuestionType type, JsonElement root)
    {
        var options = new List<FormQuestionOption>();
        if (!root.TryGetProperty("options", out var el) || el.ValueKind != JsonValueKind.Array || el.GetArrayLength() == 0)
            return options;

        if (type is not (FormQuestionType.Select or FormQuestionType.MultiSelect))
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_FormQuestion_OptionsInvalidForType, "Options are only supported for Select and MultiSelect questions.");
        if (el.GetArrayLength() > MaxOptions)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_FormQuestion_OptionsInvalid, $"At most {MaxOptions} options.");

        foreach (var item in el.EnumerateArray())
        {
            var key = GetString(item, "key")?.Trim();
            var label = GetString(item, "label")?.Trim();
            if (string.IsNullOrEmpty(key) || key.Length > 100 || string.IsNullOrEmpty(label) || label.Length > 200)
                throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_FormQuestion_OptionsInvalid, "Every option needs a key (max 100) and a label (max 200).");
            if (options.Any(o => o.Key.Equals(key, StringComparison.OrdinalIgnoreCase)))
                throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_FormQuestion_OptionsInvalid, $"Duplicate option key '{key}'.");
            options.Add(new FormQuestionOption(key, label));
        }
        return options;
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
        foreach (var option in Options) yield return option;
    }
}
