using System.Text.Json;
using Notrelix.Application.Common.Tenancy;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.BoardItems.Services;
using Notrelix.Application.Features.WorkManagement.Boards.Queries.GetFullBoard;
using Notrelix.Application.Features.WorkManagement.Common;
using Notrelix.Application.Features.WorkManagement.Common.DTOs;

namespace Notrelix.Application.Features.WorkManagement.Transfer;

public sealed record ExportTable(IReadOnlyList<string> Headers, IReadOnlyList<object?[]> Rows);

/// <summary>
/// Reads the rows of an export the way the board reads them: the same filter and sort, the same live root items
/// (archived and deleted ones are left out). It runs under the caller's own board visibility, which the caller
/// has already checked; the board's fields are all visible to everyone who can see the board (no field has its
/// own view right), so none is left out.
/// </summary>
public sealed class BoardExportBuilder(
    IWorkManagementDbContext context, IBoardItemPageReader pages, IBoardItemSummaryBuilder summaries, IActorLookupService actors)
{
    public const string ScopeView = "view";
    public const string ScopeAll = "all";
    private const int PageSize = PageCursor.MaxLimit;

    /// <summary>Items in the current view and in the whole board (live root items).</summary>
    public async Task<(int View, int All)> CountAsync(Guid boardId, string? filter, string? sort, CancellationToken ct)
    {
        var (fields, groupIds, lane) = await LoadAsync(boardId, ct);
        var read = await pages.ReadAsync(boardId, groupIds, lane, GetFullBoardQueryHandler.CriteriaFieldsOf(fields), BoardItemCriteria.Parse(filter, sort), null, null, 1, ct);
        return (read.Sum(p => p.FilteredTotal), read.Sum(p => p.Total));
    }

    public async Task<ExportTable> BuildAsync(
        Guid boardId, string scope, string? filter, string? sort, string locale, Action<int, int>? progress, CancellationToken ct)
    {
        var (fields, groupIds, lane) = await LoadAsync(boardId, ct);
        var criteria = scope == ScopeAll ? BoardItemCriteria.None : BoardItemCriteria.Parse(filter, sort);
        var criteriaFields = GetFullBoardQueryHandler.CriteriaFieldsOf(fields);
        var live = fields.Where(f => f.DeletedAt == null).OrderBy(f => f.Position).ToList();
        var status = WorkItemSemanticFields.ResolveStatusField(live);
        var priority = WorkItemSemanticFields.ResolvePriorityField(live);
        var title = live.FirstOrDefault(f => f.IsSystem && f.Type == FieldType.Text);
        var groups = await context.BoardGroups.AsNoTracking().Where(g => g.BoardId == boardId).ToDictionaryAsync(g => g.Id, g => g.Title, ct);
        var labels = Copy(locale);

        // Columns: key, title, group, status, dates, labels, people, then every other field of the board.
        var custom = live.Where(f => !f.IsSystem && f.Type is not (FieldType.Formula or FieldType.Rollup or FieldType.Vote)).ToList();
        var headers = new List<string>
        {
            labels.Key, title?.Name ?? labels.Title, labels.Group, status?.Name ?? labels.Status, labels.Assignees, labels.Start, labels.Due, labels.Completed, labels.Labels, labels.Created,
        };
        headers.AddRange(custom.Select(f => f.Name));

        var laneKeys = lane is null ? [""] : lane.OptionOrder.Select(id => id.ToString()).Append("").ToList();
        var rows = new List<object?[]>();
        var total = (await pages.ReadAsync(boardId, groupIds, lane, criteriaFields, criteria, null, null, 1, ct)).Sum(p => p.FilteredTotal);
        progress?.Invoke(0, total);
        foreach (var laneKey in laneKeys)
        {
            string? cursor = null;
            do
            {
                var page = (await pages.ReadAsync(boardId, groupIds, lane, criteriaFields, criteria, laneKey, cursor, PageSize, ct)).Single();
                cursor = page.NextCursor;
                if (page.ItemIds.Count == 0) continue;
                var loaded = await context.BoardItems.AsNoTracking().Where(i => page.ItemIds.Contains(i.Id)).ToListAsync(ct);
                var byId = loaded.ToDictionary(i => i.Id);
                var items = page.ItemIds.Where(byId.ContainsKey).Select(id => byId[id]).ToList();
                var built = await summaries.BuildAsync(fields, items, ct);
                var people = await PeopleAsync(built, custom, ct);
                foreach (var s in built)
                {
                    var item = byId[s.Id];
                    var row = new List<object?>
                    {
                        s.ItemKey, s.Name, groups.GetValueOrDefault(item.GroupId), OptionName(status, s.StatusOptionId),
                        string.Join("; ", s.Members.Select(m => m.Name)), s.StartedAt, s.DueAt, s.CompletedAt,
                        string.Join("; ", s.Labels.Select(l => l.Name)), new DateTimeOffset(DateTime.SpecifyKind(s.CreatedAt, DateTimeKind.Utc)),
                    };
                    foreach (var f in custom)
                        row.Add(f.Id == priority?.Id ? OptionName(priority, s.PriorityOptionId) : Cell(f, s, people));
                    rows.Add(row.ToArray());
                }

                progress?.Invoke(rows.Count, total);
            }
            while (cursor is not null);
        }

        return new ExportTable(headers, rows);
    }

    private async Task<(List<BoardField> Fields, List<Guid> GroupIds, CriteriaField? Lane)> LoadAsync(Guid boardId, CancellationToken ct)
    {
        var fields = await context.BoardFields.AsNoTracking().Include(f => f.Options).Where(f => f.BoardId == boardId).ToListAsync(ct);
        var groupIds = await context.BoardGroups.AsNoTracking()
            .Where(g => g.BoardId == boardId && g.DeletedAt == null && !g.IsArchived).Select(g => g.Id).ToListAsync(ct);
        return (fields, groupIds, GetFullBoardQueryHandler.LaneFieldOf(fields));
    }

    private static string? OptionName(BoardField? field, Guid? optionId) =>
        optionId is null ? null : field?.Options.FirstOrDefault(o => o.Id == optionId)?.Name;

    private async Task<Dictionary<Guid, string>> PeopleAsync(List<BoardItemSummaryDto> items, List<BoardField> custom, CancellationToken ct)
    {
        var personFields = custom.Where(f => f.Type == FieldType.Person).Select(f => f.Id.ToString()).ToList();
        if (personFields.Count == 0) return [];
        var ids = new HashSet<Guid>();
        foreach (var s in items)
            foreach (var f in personFields)
                if (s.FieldValues?.TryGetValue(f, out var v) == true)
                    foreach (var id in Ids(v)) ids.Add(id);
        return (await actors.FindManyAsync(ids.ToList(), ct)).ToDictionary(a => a.UserId, a => a.Name);
    }

    private static IEnumerable<Guid> Ids(JsonElement v) => v.ValueKind switch
    {
        JsonValueKind.String when Guid.TryParse(v.GetString(), out var id) => [id],
        JsonValueKind.Array => v.EnumerateArray().Where(e => e.ValueKind == JsonValueKind.String && Guid.TryParse(e.GetString(), out _)).Select(e => Guid.Parse(e.GetString()!)),
        _ => [],
    };

    private static object? Cell(BoardField field, BoardItemSummaryDto item, Dictionary<Guid, string> people)
    {
        if (item.FieldValues is null || !item.FieldValues.TryGetValue(field.Id.ToString(), out var v) || v.ValueKind == JsonValueKind.Null) return null;
        switch (field.Type)
        {
            case FieldType.Number: return v.TryGetDecimal(out var n) ? n : null;
            case FieldType.Checkbox: return v.ValueKind is JsonValueKind.True or JsonValueKind.False ? v.GetBoolean() : null;
            case FieldType.Date:
                return v.ValueKind == JsonValueKind.String && DateTimeOffset.TryParse(v.GetString(), System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal, out var d) ? d.ToUniversalTime() : null;
            case FieldType.Select or FieldType.Status:
                return v.ValueKind == JsonValueKind.String && Guid.TryParse(v.GetString(), out var o) ? field.Options.FirstOrDefault(x => x.Id == o)?.Name : null;
            case FieldType.MultiSelect:
                return string.Join("; ", Ids(v).Select(id => field.Options.FirstOrDefault(x => x.Id == id)?.Name).Where(x => x is not null));
            case FieldType.Person:
                return string.Join("; ", Ids(v).Select(id => people.GetValueOrDefault(id)).Where(x => x is not null));
            default: return v.ValueKind == JsonValueKind.String ? v.GetString() : v.GetRawText();
        }
    }

    private sealed record Words(string Key, string Title, string Group, string Status, string Assignees, string Start, string Due, string Completed, string Labels, string Created);

    private static Words Copy(string locale) => locale == "en"
        ? new("Key", "Title", "Group", "Status", "Assignees", "Start date", "Due date", "Completed at", "Labels", "Created at")
        : new("Mã việc", "Tên việc", "Nhóm", "Trạng thái", "Người phụ trách", "Ngày bắt đầu", "Hạn chót", "Hoàn thành lúc", "Nhãn", "Tạo lúc");
}
