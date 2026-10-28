using System.Globalization;
using System.Text.Json;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Common;
using Notrelix.Application.Features.WorkManagement.Common.DTOs;
using Notrelix.Domain.WorkManagement.Formulas;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Services;

/// <summary>The custom values and the computed values of one Item, keyed by field id.</summary>
public sealed record BoardItemFieldValues(
    IReadOnlyDictionary<string, JsonElement> Stored,
    IReadOnlyDictionary<string, ComputedValueDto> Computed);

/// <summary>
/// Read projection of the Board's custom field values and its Formula / Rollup fields
/// (additive; computed values are evaluated here on every read and never stored). Set-based:
/// one query for the values of the requested Items and their sub-items, one for the sub-items.
/// A computed value that cannot be computed is an explicit error state, never a guess.
/// </summary>
public static class BoardItemFieldProjection
{
    public const string ErrUnknownField = "unknown-field";
    public const string ErrCycle = "cycle";
    public const string ErrUnsupported = "unsupported-type";
    public const string ErrInvalidDefinition = "invalid-definition";

    public static async Task<IReadOnlyDictionary<Guid, BoardItemFieldValues>> LoadAsync(
        IWorkManagementDbContext context,
        IReadOnlyCollection<BoardField> boardFields,
        IReadOnlyCollection<Guid> itemIds,
        CancellationToken cancellationToken)
    {
        if (itemIds.Count == 0) return new Dictionary<Guid, BoardItemFieldValues>();

        var fields = boardFields.Where(f => f.DeletedAt == null).OrderBy(f => f.Position).ToList();
        var status = WorkItemSemanticFields.ResolveStatusField(fields);
        var priority = WorkItemSemanticFields.ResolvePriorityField(fields);
        var computedFields = fields.Where(f => f.Type is FieldType.Formula or FieldType.Rollup).ToList();
        var storedFields = fields
            .Where(f => f.Type is not (FieldType.Formula or FieldType.Rollup) && f.Id != status?.Id && f.Id != priority?.Id)
            .ToList();

        var hasRollup = computedFields.Any(f => f.Type == FieldType.Rollup);
        var children = hasRollup
            ? (await context.BoardItems.AsNoTracking()
                .Where(child => child.ParentItemId != null && itemIds.Contains(child.ParentItemId.Value) && !child.IsArchived && child.DeletedAt == null)
                .Select(child => new { child.Id, ParentId = child.ParentItemId!.Value })
                .ToListAsync(cancellationToken))
                .GroupBy(child => child.ParentId)
                .ToDictionary(group => group.Key, group => group.Select(child => child.Id).ToList())
            : new Dictionary<Guid, List<Guid>>();

        var fieldIds = fields.Where(f => f.Type is not (FieldType.Formula or FieldType.Rollup)).Select(f => f.Id).ToList();
        var valueItemIds = itemIds.Concat(children.Values.SelectMany(ids => ids)).Distinct().ToList();
        var rows = fieldIds.Count == 0
            ? []
            : await context.BoardItemValues.AsNoTracking()
                .Where(value => valueItemIds.Contains(value.ItemId) && fieldIds.Contains(value.FieldId))
                .ToListAsync(cancellationToken);
        var stored = new Dictionary<(Guid Item, Guid Field), JsonElement>();
        foreach (var row in rows)
        {
            if (row.Value.IsEmpty) continue;
            using var doc = JsonDocument.Parse(row.Value.Data.Value);
            stored[(row.ItemId, row.FieldId)] = doc.RootElement.Clone();
        }

        var evaluator = new Evaluator(fields, stored, children);
        var result = new Dictionary<Guid, BoardItemFieldValues>(itemIds.Count);
        foreach (var itemId in itemIds)
        {
            var own = storedFields
                .Where(f => stored.ContainsKey((itemId, f.Id)))
                .ToDictionary(f => f.Id.ToString(), f => stored[(itemId, f.Id)]);
            var computed = computedFields.ToDictionary(f => f.Id.ToString(), f => ToDto(evaluator.Value(itemId, f)));
            result[itemId] = new BoardItemFieldValues(own, computed);
        }

        return result;
    }

    private static ComputedValueDto ToDto(FormulaValue value) => value.Kind switch
    {
        FormulaValueKind.Error => new ComputedValueDto("error", null, value.Text),
        FormulaValueKind.Empty => new ComputedValueDto("empty", null, null),
        FormulaValueKind.Number => new ComputedValueDto("ok", JsonSerializer.SerializeToElement(value.Number), null),
        FormulaValueKind.Text => new ComputedValueDto("ok", JsonSerializer.SerializeToElement(value.Text), null),
        FormulaValueKind.Boolean => new ComputedValueDto("ok", JsonSerializer.SerializeToElement(value.Flag), null),
        _ => new ComputedValueDto("ok", JsonSerializer.SerializeToElement(value.Date!.Value.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture)), null),
    };

    private sealed class Evaluator(
        IReadOnlyList<BoardField> fields,
        IReadOnlyDictionary<(Guid Item, Guid Field), JsonElement> stored,
        IReadOnlyDictionary<Guid, List<Guid>> children)
    {
        private readonly Dictionary<(Guid, Guid), FormulaValue> _memo = [];
        private readonly HashSet<(Guid, Guid)> _active = [];

        public FormulaValue Value(Guid itemId, BoardField field)
        {
            var key = (itemId, field.Id);
            if (_memo.TryGetValue(key, out var done)) return done;
            // A cycle is rejected on save; one that slipped in (rename, old data) is an error, not a loop.
            if (!_active.Add(key)) return FormulaValue.Error(ErrCycle);
            try
            {
                var value = field.Type switch
                {
                    FieldType.Formula => EvaluateFormula(itemId, field),
                    FieldType.Rollup => EvaluateRollup(itemId, field),
                    _ => Stored(itemId, field),
                };
                _memo[key] = value;
                return value;
            }
            finally
            {
                _active.Remove(key);
            }
        }

        private FormulaValue EvaluateFormula(Guid itemId, BoardField field)
        {
            FormulaNode node;
            try
            {
                node = FormulaLanguage.Parse(ComputedFieldGraph.ExpressionOf(field.Settings.Data.Value));
            }
            catch (Exception ex) when (ex is BusinessRuleException or JsonException)
            {
                return FormulaValue.Error(ErrInvalidDefinition);
            }

            return FormulaEvaluator.Evaluate(node, name =>
                fields.FirstOrDefault(f => string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase)) is { } target
                    ? Value(itemId, target)
                    : FormulaValue.Error(ErrUnknownField));
        }

        private FormulaValue EvaluateRollup(Guid itemId, BoardField field)
        {
            RollupSpec spec;
            try
            {
                spec = RollupSpec.Parse(field.Settings.Data.Value);
            }
            catch (BusinessRuleException)
            {
                return FormulaValue.Error(ErrInvalidDefinition);
            }

            var subItems = children.GetValueOrDefault(itemId) ?? [];
            if (spec.FieldId is null) return spec.Aggregate(subItems.Count, []);
            var source = fields.FirstOrDefault(f => f.Id == spec.FieldId);
            return source is null
                ? FormulaValue.Error(ErrUnknownField)
                : spec.Aggregate(subItems.Count, subItems.Select(child => Value(child, source)));
        }

        private FormulaValue Stored(Guid itemId, BoardField field)
        {
            if (!stored.TryGetValue((itemId, field.Id), out var json)) return FormulaValue.Empty;
            switch (field.Type)
            {
                case FieldType.Number:
                    return json.ValueKind == JsonValueKind.Number && json.TryGetDecimal(out var number) ? FormulaValue.Of(number) : FormulaValue.Error(ErrUnsupported);
                case FieldType.Text or FieldType.LongText or FieldType.Link:
                    return json.ValueKind == JsonValueKind.String ? FormulaValue.Of(json.GetString()!) : FormulaValue.Empty;
                case FieldType.Checkbox:
                    return json.ValueKind is JsonValueKind.True or JsonValueKind.False ? FormulaValue.Of(json.GetBoolean()) : FormulaValue.Empty;
                case FieldType.Date:
                    return json.ValueKind == JsonValueKind.String && DateTimeOffset.TryParse(json.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var date)
                        ? FormulaValue.Of(date) : FormulaValue.Empty;
                case FieldType.Select or FieldType.Status:
                    return json.ValueKind == JsonValueKind.String && Guid.TryParse(json.GetString(), out var optionId)
                        ? field.Options.FirstOrDefault(o => o.Id == optionId) is { } option ? FormulaValue.Of(option.Name) : FormulaValue.Empty
                        : FormulaValue.Empty;
                case FieldType.MultiSelect:
                    return json.ValueKind == JsonValueKind.Array
                        ? FormulaValue.Of(string.Join(", ", json.EnumerateArray()
                            .Select(e => e.ValueKind == JsonValueKind.String && Guid.TryParse(e.GetString(), out var id) ? field.Options.FirstOrDefault(o => o.Id == id)?.Name : null)
                            .OfType<string>()))
                        : FormulaValue.Empty;
                default:
                    return FormulaValue.Error(ErrUnsupported);
            }
        }
    }
}
