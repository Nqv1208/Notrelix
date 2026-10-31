using System.Globalization;
using System.Text;
using System.Text.Json;
using Notrelix.Application.Features.WorkManagement.Common;
using Notrelix.Domain.WorkManagement.BoardGroups;
using Notrelix.Domain.WorkManagement.Fields;
using DomainJson = Notrelix.Domain.SharedKernel.JsonValue;

namespace Notrelix.Application.Features.WorkManagement.Transfer;

/// <summary>A place a file column can land: the item's title, its group, or one of the board's fields.</summary>
/// <param name="LockedReason">Why a field cannot be imported (a stable code the client words); null when importable.</param>
public sealed record ImportTarget(string Id, string Name, string Type, bool Required, string? LockedReason);

/// <param name="Column">0-based column of the file.</param>
/// <param name="Target">A field id, <c>$title</c> or <c>$group</c>; a column left out is not listed.</param>
public sealed record ColumnMap(int Column, string Target);

/// <param name="Status">ok, unknown-values (some options are not on the board) or incompatible (the column cannot be read as the field's type).</param>
public sealed record ColumnCheck(int Column, string Target, string Status, int Count, IReadOnlyList<string> Examples);

public sealed record ImportRowError(int Row, string Column, string Target, string? Value, string Code);

public sealed record PlannedItem(int Row, string Title, Guid GroupId, IReadOnlyList<(BoardField Field, FieldValue Value)> Values);

public sealed record SampleRow(int Row, IReadOnlyDictionary<string, string?> Cells, IReadOnlyList<string> ErrorTargets);

public sealed record ImportEvaluation(
    IReadOnlyList<ColumnCheck> Checks,
    IReadOnlyList<ImportRowError> Errors,
    int ErrorCount,
    int ErrorRows,
    IReadOnlyList<PlannedItem> Items,
    IReadOnlyList<SampleRow> Sample);

/// <summary>What the board looks like to an import: its live fields and groups.</summary>
public sealed record ImportSchema(IReadOnlyList<BoardField> Fields, IReadOnlyList<BoardGroup> Groups)
{
    public BoardField? TitleField => Fields.FirstOrDefault(f => f.IsSystem && f.Type == FieldType.Text);
}

/// <summary>
/// Decides what a file would do to a board. The same code answers the mapping check, the preview and the real
/// import, so the file that previews clean is the file that imports. Every value is turned into the stored form
/// and checked by the Domain's own field rules; nothing is written here.
/// </summary>
public static class ImportPlanner
{
    public const string Title = "$title";
    public const string Group = "$group";
    public const string Skip = "$skip";
    public const int MaxStoredErrors = 5_000;

    public const string LockedPerson = "person";
    public const string LockedComputed = "computed";
    public const string LockedVote = "vote";

    public static IReadOnlyList<ImportTarget> Targets(ImportSchema schema)
    {
        var targets = new List<ImportTarget> { new(Title, "Title", nameof(FieldType.Text), true, null), new(Group, "Group", nameof(FieldType.Select), false, null) };
        var title = schema.TitleField;
        foreach (var f in schema.Fields.OrderBy(f => f.Position))
        {
            if (title is not null && f.Id == title.Id) continue;
            var locked = f.Type switch
            {
                FieldType.Person => LockedPerson,
                FieldType.Formula or FieldType.Rollup => LockedComputed,
                FieldType.Vote => LockedVote,
                _ => f.IsSystem && f.Type is not (FieldType.Status or FieldType.Date) ? LockedPerson : null,
            };
            targets.Add(new ImportTarget(f.Id.ToString(), f.Name, f.Type.ToString(), false, locked));
        }

        return targets;
    }

    public static IReadOnlyList<ColumnMap> Suggest(IReadOnlyList<string> headers, IReadOnlyList<ImportTarget> targets)
    {
        var map = new List<ColumnMap>();
        var used = new HashSet<string>();
        string[] titleWords = ["title", "name", "task", "summary", "tieu de", "ten viec", "ten", "cong viec"];
        string[] dueWords = ["due", "due date", "deadline", "han", "han chot", "ngay het han"];
        for (var i = 0; i < headers.Count; i++)
        {
            var h = Fold(headers[i]);
            var hit = targets.Where(t => t.LockedReason is null && !used.Contains(t.Id)).FirstOrDefault(t => Fold(t.Name) == h)
                ?? (titleWords.Contains(h) && !used.Contains(Title) ? targets.First(t => t.Id == Title) : null)
                ?? (dueWords.Contains(h) ? targets.FirstOrDefault(t => t.LockedReason is null && !used.Contains(t.Id) && t.Type == nameof(FieldType.Date)) : null);
            if (hit is null) continue;
            used.Add(hit.Id);
            map.Add(new ColumnMap(i, hit.Id));
        }

        return map;
    }

    private static string Fold(string s)
    {
        var d = s.Trim().ToLowerInvariant().Replace('đ', 'd').Normalize(NormalizationForm.FormD);
        return new string(d.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray());
    }

    public static ImportEvaluation Evaluate(TabularData data, ImportSchema schema, IReadOnlyList<ColumnMap> mapping, bool collectItems)
    {
        var targets = Targets(schema).ToDictionary(t => t.Id);
        var fields = schema.Fields.ToDictionary(f => f.Id.ToString());
        var map = mapping
            .Where(m => m.Column >= 0 && m.Column < data.Headers.Count && targets.TryGetValue(m.Target, out var t) && t.LockedReason is null)
            .GroupBy(m => m.Target).Select(g => g.First())
            .ToList();
        var titleField = schema.TitleField;
        if (titleField is not null)
            map = map.Select(m => m.Target == titleField.Id.ToString() ? m with { Target = Title } : m).ToList();

        var defaultGroup = schema.Groups.Where(g => !g.IsArchived && g.DeletedAt is null).OrderBy(g => g.Position).FirstOrDefault();
        var errors = new List<ImportRowError>();
        var errorCount = 0;
        var errorRows = new HashSet<int>();
        var items = new List<PlannedItem>();
        var sample = new List<SampleRow>();
        var firstErrorRows = new List<SampleRow>();
        var unknown = map.ToDictionary(m => m.Target, _ => new Dictionary<string, int>());
        var incompatible = map.ToDictionary(m => m.Target, _ => new List<string>());
        var incompatibleCount = map.ToDictionary(m => m.Target, _ => 0);

        foreach (var row in data.Rows)
        {
            var rowErrors = new List<ImportRowError>();
            void Fail(ColumnMap m, string? raw, string code)
            {
                rowErrors.Add(new ImportRowError(row.Number, data.Headers[m.Column], m.Target, raw, code));
                if (code == "unknown-option") unknown[m.Target][raw ?? ""] = unknown[m.Target].GetValueOrDefault(raw ?? "") + 1;
                if (code is "not-number" or "not-boolean")
                {
                    incompatibleCount[m.Target]++;
                    if (incompatible[m.Target].Count < 3 && raw is not null && !incompatible[m.Target].Contains(raw)) incompatible[m.Target].Add(raw);
                }
            }

            var title = "";
            var groupId = defaultGroup?.Id ?? Guid.Empty;
            var values = new List<(BoardField, FieldValue)>();
            foreach (var m in map)
            {
                var raw = row.Cells[m.Column]?.Trim();
                if (m.Target == Title)
                {
                    title = raw ?? "";
                    if (title.Length == 0) Fail(m, null, "required");
                    else if (title.Length > 500) Fail(m, raw, "too-long");
                    continue;
                }

                if (m.Target == Group)
                {
                    if (string.IsNullOrEmpty(raw)) continue;
                    var g = schema.Groups.FirstOrDefault(x => !x.IsArchived && x.DeletedAt is null && string.Equals(x.Title.Trim(), raw, StringComparison.OrdinalIgnoreCase));
                    if (g is null) Fail(m, raw, "unknown-option"); else groupId = g.Id;
                    continue;
                }

                if (string.IsNullOrEmpty(raw)) continue;
                var field = fields[m.Target];
                var (value, code) = Convert(field, raw);
                if (code is not null) { Fail(m, raw, code); continue; }
                try { FieldValueValidator.Validate(value!, field.Type, field.Settings); }
                catch (Notrelix.Domain.Common.Exceptions.BusinessRuleException) { Fail(m, raw, "invalid-value"); continue; }
                values.Add((field, value!));
            }

            if (defaultGroup is null) rowErrors.Add(new ImportRowError(row.Number, "", Group, null, "no-group"));
            if (rowErrors.Count == 0 && collectItems) items.Add(new PlannedItem(row.Number, title, groupId, values));

            if (rowErrors.Count > 0)
            {
                errorRows.Add(row.Number);
                errorCount += rowErrors.Count;
                foreach (var e in rowErrors) if (errors.Count < MaxStoredErrors) errors.Add(e);
            }

            var cells = map.ToDictionary(m => m.Target, m => row.Cells[m.Column]);
            var line = new SampleRow(row.Number, cells, rowErrors.Select(e => e.Target).Distinct().ToList());
            if (rowErrors.Count > 0) { if (firstErrorRows.Count < 3) firstErrorRows.Add(line); }
            else if (sample.Count < 5) sample.Add(line);
        }

        var checks = map.Select(m =>
        {
            var inc = incompatibleCount[m.Target];
            if (inc > 0) return new ColumnCheck(m.Column, m.Target, "incompatible", inc, incompatible[m.Target]);
            var unk = unknown[m.Target];
            return unk.Count > 0
                ? new ColumnCheck(m.Column, m.Target, "unknown-values", unk.Count, unk.Keys.Take(3).ToList())
                : new ColumnCheck(m.Column, m.Target, "ok", 0, []);
        }).ToList();

        var preview = firstErrorRows.Count > 0 ? sample.Take(2).Concat(firstErrorRows).OrderBy(r => r.Row).ToList() : sample;
        return new ImportEvaluation(checks, errors, errorCount, errorRows.Count, items, preview);
    }

    private static (FieldValue? Value, string? Code) Convert(BoardField field, string raw)
    {
        switch (field.Type)
        {
            case FieldType.Text or FieldType.LongText:
                return (Json(raw), null);
            case FieldType.Link:
                return Uri.TryCreate(raw, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https" ? (Json(raw), null) : (null, "invalid-link");
            case FieldType.Number:
                return TryNumber(raw, out var n) ? (Raw(n.ToString(CultureInfo.InvariantCulture)), null) : (null, "not-number");
            case FieldType.Checkbox:
                return TryBool(raw, out var b) ? (Raw(b ? "true" : "false"), null) : (null, "not-boolean");
            case FieldType.Date:
                return TryDate(raw, out var d) ? (Json(d.ToString("O", CultureInfo.InvariantCulture)), null) : (null, "not-date");
            case FieldType.Status or FieldType.Select:
            {
                var option = FindOption(field, raw);
                return option is null ? (null, "unknown-option") : (Json(option.Id.ToString()), null);
            }
            case FieldType.MultiSelect:
            {
                var ids = new List<string>();
                foreach (var part in raw.Split([';', '|', '\n'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                {
                    var option = FindOption(field, part);
                    if (option is null) return (null, "unknown-option");
                    if (!ids.Contains(option.Id.ToString())) ids.Add(option.Id.ToString());
                }

                return ids.Count == 0 ? (null, null) : (Raw(JsonSerializer.Serialize(ids)), null);
            }
            default:
                return (null, "invalid-value");
        }
    }

    private static FieldOption? FindOption(BoardField field, string raw) =>
        field.Options.FirstOrDefault(o => string.Equals(o.Name.Trim(), raw.Trim(), StringComparison.OrdinalIgnoreCase));

    private static FieldValue Json(string text) => Raw(JsonSerializer.Serialize(text));

    private static FieldValue Raw(string json) => FieldValue.Create(DomainJson.Create(json));

    public static bool TryNumber(string raw, out decimal value)
    {
        var s = raw.Trim().Replace(" ", "").Replace(" ", "");
        // Vietnamese files write 2,5 and 1.234,5; English files write 2.5 and 1,234.5. The last separator is the decimal one.
        var comma = s.LastIndexOf(',');
        var dot = s.LastIndexOf('.');
        if (comma >= 0 && dot >= 0) s = comma > dot ? s.Replace(".", "").Replace(',', '.') : s.Replace(",", "");
        else if (comma >= 0) s = s.Count(c => c == ',') == 1 ? s.Replace(',', '.') : s.Replace(",", "");
        return decimal.TryParse(s, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out value);
    }

    private static bool TryBool(string raw, out bool value)
    {
        switch (raw.Trim().ToLowerInvariant())
        {
            case "true" or "1" or "yes" or "y" or "x" or "có" or "co" or "✓": value = true; return true;
            case "false" or "0" or "no" or "n" or "không" or "khong": value = false; return true;
            default: value = false; return false;
        }
    }

    private static readonly string[] DateFormats =
    [
        "yyyy-MM-dd", "yyyy-MM-dd'T'HH:mm:ss", "yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd'T'HH:mm:ssK", "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFK",
        "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "d-M-yyyy", "dd.MM.yyyy", "d.M.yyyy", "dd/MM/yyyy HH:mm", "d/M/yyyy H:mm",
    ];

    private static bool TryDate(string raw, out DateTimeOffset value) =>
        DateTimeOffset.TryParseExact(raw.Trim(), DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out value)
        && (value = value.ToUniversalTime()).Year is >= 1900 and <= 2200;
}
