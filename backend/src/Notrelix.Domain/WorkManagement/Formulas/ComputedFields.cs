using System.Text.Json;

namespace Notrelix.Domain.WorkManagement.Formulas;

/// <summary>
/// Rollup v1 settings: <c>{"function":"count|sum|avg|min|max","fieldId":"…"}</c> over the
/// Item's sub-items. <c>count</c> counts sub-items; the others aggregate the number-valued
/// field <c>fieldId</c> of the same Board.
/// </summary>
public sealed record RollupSpec(string Function, Guid? FieldId)
{
    public static readonly IReadOnlySet<string> Functions = new HashSet<string>(["count", "sum", "avg", "min", "max"], StringComparer.Ordinal);

    public static RollupSpec Parse(string settingsJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(settingsJson) ? "{}" : settingsJson);
            var root = doc.RootElement;
            var fn = root.ValueKind == JsonValueKind.Object && root.TryGetProperty("function", out var f) && f.ValueKind == JsonValueKind.String
                ? f.GetString()!.ToLowerInvariant() : null;
            if (fn is null || !Functions.Contains(fn))
                throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_FieldSettings_RollupInvalid, "A Rollup needs a function: count, sum, avg, min or max.");
            Guid? fieldId = null;
            if (root.TryGetProperty("fieldId", out var id) && id.ValueKind == JsonValueKind.String && Guid.TryParse(id.GetString(), out var parsed))
                fieldId = parsed;
            if (fn != "count" && fieldId is null)
                throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_FieldSettings_RollupInvalid, $"Rollup '{fn}' needs the 'fieldId' of the field to aggregate.");
            return new RollupSpec(fn, fn == "count" ? null : fieldId);
        }
        catch (JsonException)
        {
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_FieldSettings_InvalidJsonFormat, "Invalid JSON format in field settings.");
        }
    }

    /// <summary>Aggregates the sub-items' values; <paramref name="subItemCount"/> is the number of sub-items.</summary>
    public FormulaValue Aggregate(int subItemCount, IEnumerable<FormulaValue> values)
    {
        if (Function == "count") return FormulaValue.Of(subItemCount);
        var numbers = new List<decimal>();
        foreach (var v in values)
        {
            if (v.IsError) return v;
            if (v.Kind == FormulaValueKind.Empty) continue;
            if (v.Kind != FormulaValueKind.Number) return FormulaValue.Error(FormulaEvaluator.ErrType);
            numbers.Add(v.Number);
        }
        if (Function == "sum") return FormulaValue.Of(numbers.Sum());
        if (numbers.Count == 0) return FormulaValue.Empty;
        return Function switch
        {
            "avg" => FormulaValue.Of(numbers.Average()),
            "min" => FormulaValue.Of(numbers.Min()),
            _ => FormulaValue.Of(numbers.Max()),
        };
    }
}

/// <summary>A Board field as the computed-field graph sees it.</summary>
public sealed record ComputedFieldInput(Guid Id, string Name, Fields.FieldType Type, string SettingsJson);

/// <summary>
/// Save-time validation of the Board's computed fields: every Formula parses, references only
/// fields of the Board, every Rollup names a Board field, and no computed field depends on
/// itself, directly or through others (cycles are rejected, never evaluated).
/// </summary>
public static class ComputedFieldGraph
{
    public static string? ExpressionOf(string settingsJson)
    {
        using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(settingsJson) ? "{}" : settingsJson);
        return doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty("expression", out var e) && e.ValueKind == JsonValueKind.String
            ? e.GetString() : null;
    }

    /// <summary>The ids of the fields a Formula or Rollup depends on; throws for an unknown one.</summary>
    public static IReadOnlyList<Guid> DependenciesOf(ComputedFieldInput field, IReadOnlyList<ComputedFieldInput> board)
    {
        if (field.Type == Fields.FieldType.Rollup)
        {
            var spec = RollupSpec.Parse(field.SettingsJson);
            if (spec.FieldId is null) return [];
            if (board.All(f => f.Id != spec.FieldId))
                throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Field_FormulaUnknownField, $"Rollup '{field.Name}' aggregates a field that is not on this Board.");
            return [spec.FieldId.Value];
        }

        var node = FormulaLanguage.Parse(ExpressionOf(field.SettingsJson));
        var ids = new List<Guid>();
        foreach (var name in FormulaLanguage.References(node))
        {
            var target = board.FirstOrDefault(f => string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase));
            if (target is null)
                throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Field_FormulaUnknownField, $"Formula '{field.Name}' references [{name}], which is not a field of this Board.");
            ids.Add(target.Id);
        }
        return ids;
    }

    /// <summary>Throws the rule code of the first invalid computed field of <paramref name="board"/>.</summary>
    public static void EnsureValid(IReadOnlyList<ComputedFieldInput> board)
    {
        var computed = board.Where(f => f.Type is Fields.FieldType.Formula or Fields.FieldType.Rollup).ToList();
        var edges = computed.ToDictionary(f => f.Id, f => DependenciesOf(f, board));
        var state = new Dictionary<Guid, int>(); // 1 = on the stack, 2 = done

        void Visit(Guid id)
        {
            if (!edges.TryGetValue(id, out var next)) return;
            if (state.GetValueOrDefault(id) == 2) return;
            if (state.GetValueOrDefault(id) == 1)
                throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Field_ComputedFieldCycle, $"'{board.First(f => f.Id == id).Name}' depends on itself: computed fields cannot form a cycle.");
            state[id] = 1;
            foreach (var dep in next) Visit(dep);
            state[id] = 2;
        }

        foreach (var f in computed) Visit(f.Id);
    }
}
