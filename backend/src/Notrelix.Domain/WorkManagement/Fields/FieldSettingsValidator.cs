namespace Notrelix.Domain.WorkManagement.Fields;

public static class FieldSettingsValidator
{
    public static void Validate(FieldSettings settings, FieldType type)
    {
        Guard.NotNull(settings);

        var data = settings.Data.Value;
        FieldJsonBounds.EnsureWithin(data, FieldJsonBounds.MaxSettingsLength, WorkManagementRuleCodes.WorkManagement_FieldSettings_TooLarge, "Field settings");

        // Status fields require transitions even with empty settings.
        if (type == FieldType.Status && (string.IsNullOrWhiteSpace(data) || data == "{}"))
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_FieldSettings_StatusMustIncludeTransitions, "Status field settings must include 'transitions'.");

        // A Relation field names the one board it links to; without it the field has no meaning.
        if (type == FieldType.Relation && RelationFieldSettings.TargetBoardId(data) is null)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Relation_TargetBoardRequired, "A Relation field needs a target board.");

        if (type is FieldType.Formula or FieldType.Rollup)
        {
            // Computed fields have no valid empty settings; syntax, node cap and shape are checked here,
            // references and cycles against the Board's other fields in ComputedFieldGraph.
            try
            {
                if (type == FieldType.Formula) Formulas.FormulaLanguage.Parse(Formulas.ComputedFieldGraph.ExpressionOf(data));
                else Formulas.RollupSpec.Parse(data);
            }
            catch (System.Text.Json.JsonException)
            {
                throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_FieldSettings_InvalidJsonFormat, "Invalid JSON format in field settings.");
            }
            return;
        }

        if (string.IsNullOrWhiteSpace(data) || data == "{}")
            return;

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(data);
            var root = doc.RootElement;
            FieldRules.Validate(root);

            switch (type)
            {
                case FieldType.Number:
                    if (root.TryGetProperty("min", out var min) && min.ValueKind != System.Text.Json.JsonValueKind.Number)
                        throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_FieldSettings_NumberMinMustBeNumber, "'min' in Number field settings must be a number.");
                    if (root.TryGetProperty("max", out var max) && max.ValueKind != System.Text.Json.JsonValueKind.Number)
                        throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_FieldSettings_NumberMaxMustBeNumber, "'max' in Number field settings must be a number.");
                    break;

                case FieldType.Text:
                case FieldType.LongText:
                    if (root.TryGetProperty("maxLength", out var maxLen) && maxLen.ValueKind != System.Text.Json.JsonValueKind.Number)
                        throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_FieldSettings_TextMaxLengthMustBeNumber, "'maxLength' in Text field settings must be a number.");
                    break;

                case FieldType.Date:
                    if (root.TryGetProperty("includeTime", out var incTime) && incTime.ValueKind != System.Text.Json.JsonValueKind.True && incTime.ValueKind != System.Text.Json.JsonValueKind.False)
                        throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_FieldSettings_DateIncludeTimeMustBeBoolean, "'includeTime' in Date field settings must be a boolean.");
                    break;

                case FieldType.Status:
                    if (!root.TryGetProperty("transitions", out var _))
                        throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_FieldSettings_StatusMustIncludeTransitions, "Status field settings must include 'transitions'.");
                    break;
            }
        }
        catch (System.Text.Json.JsonException)
        {
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_FieldSettings_InvalidJsonFormat, "Invalid JSON format in field settings.");
        }
    }
}
