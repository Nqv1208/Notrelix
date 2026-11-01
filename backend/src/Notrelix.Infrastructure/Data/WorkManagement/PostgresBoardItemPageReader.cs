using System.Globalization;
using Notrelix.Application.Features.WorkManagement.BoardItems.Services;
using Notrelix.Domain.WorkManagement.Fields;

namespace Notrelix.Infrastructure.Data.WorkManagement;

internal enum KeyKind { Text, Number, Date, Int }

/// <summary>
/// PostgreSQL implementation of <see cref="IBoardItemPageReader"/>: ids of one page per lane, in the criteria's order,
/// with the lane size before and after the filter. Every value reaches the statement as a parameter; field ids,
/// operators and sort keys are matched against fixed lists, so nothing from the request becomes SQL text. The queries
/// run on the caller's connection, so row-level security applies to every Item and value they touch.
/// <para>
/// Parity with the web client's filter (WM compound filter, registry per field type): the same operators, the same
/// accent- and case-insensitive text matching, the same empty rule (empty last ascending, first descending), options
/// ordered by board order, a Person field falling back to the Item's people, the first Date field falling back to the
/// due date. Fields and operators it cannot evaluate here (Formula, Rollup, Vote, "$overdue", sorting by people or
/// several choices) are refused with a validation failure on "filter" or "sort", and the client then reads the
/// whole board and filters there.
/// </para>
/// <para>
/// shortcut: text matching and value sorts scan the group's Items in the database (no trigram or expression index),
/// bounded by the group, not by the page; add per-field indexes when a single group holds hundreds of thousands.
/// </para>
/// </summary>
public sealed class PostgresBoardItemPageReader(ApplicationDbContext context) : IBoardItemPageReader
{
    private const string NoneValue = "__none__";
    private const string TitleId = "$title";
    private const string LabelId = "$label";
    private const string AssigneeId = "$assignee";
    private const string OverdueId = "$overdue";

    private const string LiveRootItem =
        "i.board_id = @board AND i.deleted_at IS NULL AND NOT i.is_archived AND i.parent_item_id IS NULL";

    public async Task<IReadOnlyList<BoardItemLanePage>> ReadAsync(
        Guid boardId,
        IReadOnlyList<Guid> groupIds,
        CriteriaField? laneField,
        IReadOnlyList<CriteriaField> fields,
        BoardItemCriteria criteria,
        string? onlyLane,
        string? cursor,
        int limit,
        CancellationToken cancellationToken)
    {
        var keys = onlyLane is not null
            ? new List<string> { onlyLane }
            : (laneField?.OptionOrder.Select(option => option.ToString()) ?? []).Append("").ToList();
        if (groupIds.Count == 0) return keys.Select(key => new BoardItemLanePage(key, [], 0, 0, null)).ToList();

        // Equal keys keep the board's own order: the group's place, then the Item's place in it (the client sorts stably).
        // One pass over the board's live root Items (row-level security runs once per Item): each gets its lane,
        // whether it passes the filter and its sort keys; the lane sizes and the wanted window come from that.
        var builder = new Builder(fields);
        var filter = builder.Filter(criteria);
        var lane = builder.Lane(laneField);
        var keyColumns = builder.SortKeys(criteria);
        var columns = string.Concat(keyColumns.Select((key, index) => $", {key.Expression} AS s{index}"));
        var window = string.Concat(keyColumns.Select((key, index) => $"s{index} {(key.Descending ? "DESC NULLS FIRST" : "ASC NULLS LAST")}, "));
        var sortId = string.Join("~", criteria.Sorts.Select(level => $"{level.FieldId}.{(level.Descending ? "d" : "a")}"));
        var after = onlyLane is null ? null : PageCursor.Decode(cursor) is { } keyset ? builder.After(keyset, sortId, keyColumns) : null;
        var keyList = keyColumns.Count == 0
            ? "ARRAY[]::text[]"
            : $"ARRAY[{string.Join(", ", keyColumns.Select((key, index) => key.Kind == KeyKind.Date ? $"to_char(s{index}, 'YYYY-MM-DD')" : $"s{index}::text"))}]::text[]";
        // The page is what follows the cursor's position in the lane's order, one row more than asked for to tell
        // whether another page exists.
        var sql = $"""
            WITH base AS MATERIALIZED (
                SELECT i.id, g.position AS gpos, i.position AS pos, {lane} AS lane_key, {(filter is null ? "true" : $"COALESCE(({filter}), false)")} AS ok{columns}
                FROM work.board_items i JOIN work.board_groups g ON g.id = i.group_id
                WHERE {LiveRootItem} AND i.group_id = ANY(@groups)
            )
            SELECT 'T' AS kind, lane_key, NULL::uuid AS item_id, count(*)::int AS total, (count(*) FILTER (WHERE ok))::int AS filtered, NULL::text AS group_position, NULL::text AS position, NULL::text[] AS keys, 0 AS rn
            FROM base{(onlyLane is null ? "" : " WHERE lane_key = @onlyLane")} GROUP BY lane_key
            UNION ALL
            SELECT 'P', lane_key, id, 0, 0, gpos, pos, keys, rn FROM (
                SELECT id, lane_key, gpos, pos, {keyList} AS keys, row_number() OVER (PARTITION BY lane_key ORDER BY {window}gpos, pos, id)::int AS rn
                FROM base WHERE ok{(onlyLane is null ? "" : " AND lane_key = @onlyLane")}{(after is null ? "" : $" AND ({after})")}
            ) ranked WHERE rn <= @limit + 1
            ORDER BY 1 DESC, 2, 9
            """;
        var parameters = builder.Parameters;
        parameters.Add(new NpgsqlParameter("board", boardId));
        parameters.Add(new NpgsqlParameter("groups", groupIds.ToArray()));
        parameters.Add(new NpgsqlParameter("limit", limit));
        if (onlyLane is not null) parameters.Add(new NpgsqlParameter("onlyLane", onlyLane));
        var rows = await context.Database.SqlQueryRaw<PageRow>(sql, parameters.Cast<object>().ToArray()).ToListAsync(cancellationToken);

        var sizes = rows.Where(row => row.Kind == "T").ToDictionary(row => row.LaneKey, row => (row.Total, row.Filtered));
        var pages = rows.Where(row => row.Kind == "P").GroupBy(row => row.LaneKey).ToDictionary(g => g.Key, g => g.OrderBy(row => row.Rn).ToList());
        return keys
            .Select(key =>
            {
                sizes.TryGetValue(key, out var size);
                var page = pages.GetValueOrDefault(key) ?? [];
                var more = page.Count > limit;
                var kept = more ? page.Take(limit).ToList() : page;
                var last = kept.Count == 0 ? null : kept[^1];
                var next = more && last is not null
                    ? PageCursor.Encode(new LaneKeyset(sortId, last.Keys ?? [], last.GroupPosition ?? "", last.Position ?? "", last.ItemId!.Value))
                    : null;
                return new BoardItemLanePage(key, kept.Select(row => row.ItemId!.Value).ToList(), size.Total, size.Filtered, next);
            })
            .ToList();
    }

    private sealed class PageRow
    {
        public string Kind { get; set; } = "";
        public string LaneKey { get; set; } = "";
        public Guid? ItemId { get; set; }
        public int Total { get; set; }
        public int Filtered { get; set; }
        public string? GroupPosition { get; set; }
        public string? Position { get; set; }
        public string?[]? Keys { get; set; }
        public int Rn { get; set; }
    }

    /// <summary>Translates criteria into SQL text and parameters for one statement.</summary>
    private sealed class Builder(IReadOnlyList<CriteriaField> fields)
    {
        private readonly Dictionary<Guid, CriteriaField> _fields = fields.ToDictionary(field => field.Id);
        public List<NpgsqlParameter> Parameters { get; } = [];

        private string Add(object value, NpgsqlDbType type)
        {
            var name = $"p{Parameters.Count}";
            Parameters.Add(new NpgsqlParameter(name, type) { Value = value });
            return "@" + name;
        }

        private static string Fold(string text) =>
            $"btrim(replace(lower(regexp_replace(normalize({text}, NFD), '[\\u0300-\\u036f]', '', 'g')), 'đ', 'd'))";

        private static Application.Common.Exceptions.ValidationException Refuse(string property, string message) =>
            BoardItemCriteria.Refuse(property, message);

        /// <summary>The lane key of an Item: its Status option when that is still an option of the field, else the empty lane.</summary>
        public string Lane(CriteriaField? laneField)
        {
            if (laneField is null) return "''::text";
            var fp = Add(laneField.Id, NpgsqlDbType.Uuid);
            var options = Add(laneField.OptionOrder.Select(option => option.ToString()).ToArray(), NpgsqlDbType.Array | NpgsqlDbType.Text);
            var stored = $"(SELECT v.value #>> ARRAY[]::text[] FROM work.board_item_values v WHERE v.item_id = i.id AND v.field_id = {fp})";
            return $"(CASE WHEN {stored} = ANY({options}) THEN {stored} ELSE '' END)";
        }

        // ---- filter ----

        public string? Filter(BoardItemCriteria criteria)
        {
            var parts = new List<string>();
            foreach (var condition in criteria.Conditions)
            {
                var sql = Condition(condition);
                if (sql is not null) parts.Add($"({sql})");
            }

            // A condition on a field the board no longer has is ignored, as the client does.
            return parts.Count == 0 ? null : string.Join(criteria.MatchAll ? " AND " : " OR ", parts);
        }

        private string? Condition(CriteriaCondition condition)
        {
            var op = condition.Operator;
            var values = condition.Values;
            var first = values.Count > 0 ? values[0] : "";

            switch (condition.FieldId)
            {
                case TitleId:
                    return Text("i.name", op, first);
                case LabelId:
                    return SetOf("ARRAY(SELECT l.label_id::text FROM work.board_item_labels l WHERE l.item_id = i.id)", op, values);
                case AssigneeId:
                    return SetOf(Members, op, values);
                case OverdueId:
                    throw Refuse("filter", "Filtering by overdue is not available on a paged board.");
            }

            if (!Guid.TryParse(condition.FieldId, out var fieldId) || !_fields.TryGetValue(fieldId, out var field))
            {
                return null;
            }

            var fp = Add(field.Id, NpgsqlDbType.Uuid);
            var value = $"(SELECT v.value FROM work.board_item_values v WHERE v.item_id = i.id AND v.field_id = {fp})";
            var scalar = $"(SELECT v.value #>> ARRAY[]::text[] FROM work.board_item_values v WHERE v.item_id = i.id AND v.field_id = {fp})";
            switch (field.Type)
            {
                case FieldType.Text:
                case FieldType.LongText:
                case FieldType.Link:
                    return Text(scalar, op, first);
                case FieldType.Number:
                    return Number(NumberOf(value), op, first);
                case FieldType.Date:
                    return Date(DateOf(field, scalar), op, values);
                case FieldType.Checkbox:
                    return op switch
                    {
                        "is_true" => $"COALESCE({value} = 'true'::jsonb, false)",
                        "is_false" => $"NOT COALESCE({value} = 'true'::jsonb, false)",
                        _ => throw Refuse("filter", $"The operator '{op}' is not defined for a checkbox."),
                    };
                case FieldType.Select:
                case FieldType.Status:
                    {
                        var single = $"COALESCE({scalar}, '')";
                        return op switch
                        {
                            "equals" => $"({single} <> '' AND {single} = {Add(first, NpgsqlDbType.Text)})",
                            "not_equals" => $"({single} = '' OR {single} <> {Add(first, NpgsqlDbType.Text)})",
                            _ => SetOf($"CASE WHEN {single} = '' THEN ARRAY[]::text[] ELSE ARRAY[{single}] END", op, values),
                        };
                    }
                case FieldType.MultiSelect:
                    return SetOf(
                        $"COALESCE((SELECT CASE WHEN jsonb_typeof(v.value) = 'array' THEN ARRAY(SELECT jsonb_array_elements_text(v.value)) END FROM work.board_item_values v WHERE v.item_id = i.id AND v.field_id = {fp}), ARRAY[]::text[])",
                        op, values);
                case FieldType.Person:
                    {
                        var stored = $"(SELECT CASE WHEN jsonb_typeof(v.value) = 'array' THEN ARRAY(SELECT jsonb_array_elements_text(v.value)) WHEN jsonb_typeof(v.value) = 'string' THEN ARRAY[v.value #>> ARRAY[]::text[]] END FROM work.board_item_values v WHERE v.item_id = i.id AND v.field_id = {fp})";
                        var storedList = $"(SELECT CASE WHEN jsonb_typeof(v.value) = 'array' THEN ARRAY(SELECT jsonb_array_elements_text(v.value)) END FROM work.board_item_values v WHERE v.item_id = i.id AND v.field_id = {fp})";
                        var people = field.PersonSource switch
                        {
                            PersonSource.Members => Members,
                            PersonSource.StoredOrMembers => $"COALESCE(NULLIF({storedList}, ARRAY[]::text[]), {Members})",
                            _ => $"COALESCE({stored}, ARRAY[]::text[])",
                        };
                        return SetOf(people, op, values);
                    }
                default:
                    throw Refuse("filter", $"A {field.Type} field cannot be filtered on a paged board.");
            }
        }

        private const string Members = "ARRAY(SELECT m.user_id::text FROM work.board_item_members m WHERE m.item_id = i.id)";

        private string Text(string text, string op, string first)
        {
            var empty = $"({text} IS NULL OR {text} = '')";
            return op switch
            {
                "is_empty" => empty,
                "is_not_empty" => $"NOT {empty}",
                "contains" => $"(NOT {empty} AND strpos({Fold(text)}, {Fold(Add(first, NpgsqlDbType.Text))}) > 0)",
                "not_contains" => $"({empty} OR strpos({Fold(text)}, {Fold(Add(first, NpgsqlDbType.Text))}) = 0)",
                "equals" => $"(NOT {empty} AND {Fold(text)} = {Fold(Add(first, NpgsqlDbType.Text))})",
                "not_equals" => $"({empty} OR {Fold(text)} <> {Fold(Add(first, NpgsqlDbType.Text))})",
                _ => throw Refuse("filter", $"The operator '{op}' is not defined for text."),
            };
        }

        private static string NumberOf(string value) =>
            $"(CASE WHEN jsonb_typeof({value}) = 'number' THEN ({value} #>> ARRAY[]::text[])::numeric END)";

        private string Number(string number, string op, string first)
        {
            var has = decimal.TryParse(first, NumberStyles.Float, CultureInfo.InvariantCulture, out var operand);
            var parameter = has ? Add(operand, NpgsqlDbType.Numeric) : null;
            switch (op)
            {
                case "is_empty": return $"{number} IS NULL";
                case "is_not_empty": return $"{number} IS NOT NULL";
                case "equals": return parameter is null ? "false" : $"{number} = {parameter}";
                case "not_equals": return parameter is null ? "true" : $"({number} IS NULL OR {number} <> {parameter})";
                case "gt" or "gte" or "lt" or "lte":
                    {
                        if (parameter is null) return "false";
                        var symbol = op switch { "gt" => ">", "gte" => ">=", "lt" => "<", _ => "<=" };
                        return $"({number} IS NOT NULL AND {number} {symbol} {parameter})";
                    }
                default: throw Refuse("filter", $"The operator '{op}' is not defined for a number.");
            }
        }

        /// <summary>A day (date) of the field's value. A field named like a due date mirrors the Item's due date; the first Date field falls back to it.</summary>
        private static string DateOf(CriteriaField field, string scalar)
        {
            const string due = "to_char(i.due_at AT TIME ZONE 'UTC', 'YYYY-MM-DD')";
            var text = field.MirrorsDue ? due
                : field.IsDueField ? $"left(COALESCE({scalar}, {due}), 10)"
                : $"left({scalar}, 10)";
            return $"(CASE WHEN {text} ~ '^[0-9][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9]$' THEN {text}::date END)";
        }

        private string Date(string day, string op, IReadOnlyList<string> values)
        {
            static bool TryDay(string text, out DateOnly date) =>
                DateOnly.TryParseExact(text.Length >= 10 ? text[..10] : text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

            var first = values.Count > 0 ? values[0] : "";
            var hasFirst = TryDay(first, out var from);
            switch (op)
            {
                case "is_empty": return $"{day} IS NULL";
                case "is_not_empty": return $"{day} IS NOT NULL";
                case "equals": return hasFirst ? $"{day} = {Add(from, NpgsqlDbType.Date)}" : "false";
                case "not_equals": return hasFirst ? $"({day} IS NULL OR {day} <> {Add(from, NpgsqlDbType.Date)})" : "true";
                case "before": return hasFirst ? $"{day} < {Add(from, NpgsqlDbType.Date)}" : "false";
                case "after": return hasFirst ? $"{day} > {Add(from, NpgsqlDbType.Date)}" : "false";
                case "between":
                    {
                        var hasTo = values.Count > 1 && TryDay(values[1], out _);
                        TryDay(values.Count > 1 ? values[1] : "", out var to);
                        return hasFirst && hasTo ? $"({day} >= {Add(from, NpgsqlDbType.Date)} AND {day} <= {Add(to, NpgsqlDbType.Date)})" : "false";
                    }
                default: throw Refuse("filter", $"The operator '{op}' is not defined for a date.");
            }
        }

        private string SetOf(string array, string op, IReadOnlyList<string> values)
        {
            switch (op)
            {
                case "is_empty": return $"cardinality({array}) = 0";
                case "is_not_empty": return $"cardinality({array}) > 0";
                case "has_any":
                    {
                        var none = values.Contains(NoneValue) ? $" OR cardinality({array}) = 0" : "";
                        return $"({array} && {Add(values.ToArray(), NpgsqlDbType.Array | NpgsqlDbType.Text)}{none})";
                    }
                case "has_all":
                    return values.Count == 0 ? "false" : $"{array} @> {Add(values.ToArray(), NpgsqlDbType.Array | NpgsqlDbType.Text)}";
                default: throw Refuse("filter", $"The operator '{op}' is not defined for this field.");
            }
        }

        // ---- order ----

        /// <summary>
        /// The condition for "after the cursor's position" in the lane's order: for each sort level, equal on the earlier
        /// levels and later on this one (empty values last ascending, first descending), then the group place, place and id.
        /// </summary>
        public string After(LaneKeyset keyset, string sortId, IReadOnlyList<SortKeyColumn> keyColumns)
        {
            if (keyset.Sort != sortId || keyset.Keys.Count != keyColumns.Count) throw Refuse("cursor", "The cursor does not belong to this sort.");
            var parameters = keyColumns.Select((key, index) => keyset.Keys[index] is { } text ? Add(KeyValue(key.Kind, text), KeyType(key.Kind)) : null).ToList();
            string Equal(int index) => parameters[index] is null ? $"s{index} IS NULL" : $"s{index} = {Cast(keyColumns[index], parameters[index]!)}";
            var alternatives = new List<string>();
            for (var i = 0; i < keyColumns.Count; i++)
            {
                var key = keyColumns[i];
                string? later;
                if (key.Descending) later = parameters[i] is null ? $"s{i} IS NOT NULL" : $"s{i} < {Cast(key, parameters[i]!)}";
                else later = parameters[i] is null ? null : $"(s{i} > {Cast(key, parameters[i]!)} OR s{i} IS NULL)";
                if (later is null) continue;
                alternatives.Add($"({string.Concat(Enumerable.Range(0, i).Select(j => Equal(j) + " AND "))}{later})");
            }

            var gpos = Add(keyset.GroupPosition, NpgsqlDbType.Text);
            var pos = Add(keyset.Position, NpgsqlDbType.Text);
            var id = Add(keyset.Id, NpgsqlDbType.Uuid);
            var allEqual = string.Concat(Enumerable.Range(0, keyColumns.Count).Select(j => Equal(j) + " AND "));
            alternatives.Add($"({allEqual}(gpos > {gpos} OR (gpos = {gpos} AND (pos > {pos} OR (pos = {pos} AND id > {id})))))");
            return string.Join(" OR ", alternatives);
        }

        private static string Cast(SortKeyColumn key, string parameter) =>
            key.Kind == KeyKind.Text ? $"({parameter} {TextCollation})" : parameter;

        private static NpgsqlDbType KeyType(KeyKind kind) => kind switch
        {
            KeyKind.Number => NpgsqlDbType.Numeric,
            KeyKind.Date => NpgsqlDbType.Date,
            KeyKind.Int => NpgsqlDbType.Integer,
            _ => NpgsqlDbType.Text,
        };

        private static object KeyValue(KeyKind kind, string text)
        {
            try
            {
                return kind switch
                {
                    KeyKind.Number => decimal.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture),
                    KeyKind.Date => DateOnly.ParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture),
                    KeyKind.Int => int.Parse(text, CultureInfo.InvariantCulture),
                    _ => text,
                };
            }
            catch (FormatException)
            {
                throw Refuse("cursor", "The cursor is not valid.");
            }
        }

        public sealed record SortKeyColumn(string Expression, bool Descending, KeyKind Kind);

        /// <summary>The sort levels' key expressions, in order; empty values sort last ascending, first descending.</summary>
        public List<SortKeyColumn> SortKeys(BoardItemCriteria criteria)
        {
            var keys = new List<SortKeyColumn>();
            foreach (var level in criteria.Sorts)
            {
                var key = SortKey(level.FieldId);
                if (key is null) continue; // a field the board no longer has: ignored, as the client does
                keys.Add(new SortKeyColumn(key.Value.Expression, level.Descending, key.Value.Kind));
            }

            return keys;
        }

        private const string TextCollation = "COLLATE work.nrx_text_sort";

        private (string Expression, KeyKind Kind)? SortKey(string fieldId)
        {
            if (fieldId == TitleId) return ($"NULLIF(i.name, '') {TextCollation}", KeyKind.Text);
            if (fieldId is LabelId or AssigneeId or OverdueId)
            {
                throw Refuse("sort", "This field cannot be sorted on a paged board.");
            }

            if (!Guid.TryParse(fieldId, out var id) || !_fields.TryGetValue(id, out var field)) return null;

            var fp = Add(field.Id, NpgsqlDbType.Uuid);
            var value = $"(SELECT v.value FROM work.board_item_values v WHERE v.item_id = i.id AND v.field_id = {fp})";
            var scalar = $"(SELECT v.value #>> ARRAY[]::text[] FROM work.board_item_values v WHERE v.item_id = i.id AND v.field_id = {fp})";
            switch (field.Type)
            {
                case FieldType.Text or FieldType.LongText or FieldType.Link:
                    return ($"NULLIF({scalar}, '') {TextCollation}", KeyKind.Text);
                case FieldType.Number:
                    return (NumberOf(value), KeyKind.Number);
                case FieldType.Date:
                    return (DateOf(field, scalar), KeyKind.Date);
                case FieldType.Checkbox:
                    return ($"(CASE WHEN {value} IS NULL THEN NULL WHEN {value} = 'true'::jsonb THEN 1 ELSE 0 END)", KeyKind.Int);
                case FieldType.Select or FieldType.Status:
                    {
                        var order = Add(field.OptionOrder.Select(option => option.ToString()).ToArray(), NpgsqlDbType.Array | NpgsqlDbType.Text);
                        return ($"(CASE WHEN COALESCE({scalar}, '') = '' THEN NULL ELSE COALESCE(array_position({order}, {scalar}), 2147483647) END)", KeyKind.Int);
                    }
                default:
                    throw Refuse("sort", $"A {field.Type} field cannot be sorted on a paged board.");
            }
        }
    }
}
