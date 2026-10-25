using System.Text.Json;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Common;
using Notrelix.Application.Features.WorkManagement.Public.Reports;
using Notrelix.Domain.WorkManagement.Fields;
using Notrelix.Domain.WorkManagement.Items;

namespace Notrelix.Infrastructure.CrossContext.Analytics.WorkManagement;

/// <summary>
/// Work Management-owned implementation of the published work report measures consumed by the Analytics
/// board and workspace reports. Reads only Work persistence through the Work port; the caller supplies the
/// readable Board ids and this adapter never widens them. Every measure is one GROUP BY over the Boards'
/// Items (and their Status values), so the cost follows the number of groups, not the number of Items.
/// </summary>
public sealed class WorkReportFactsAdapter : IWorkReportFacts
{
    private const int MaxAssignees = 50;
    private static readonly string[] CategoryOrder =
    [
        WorkReportStatusCategories.Todo,
        WorkReportStatusCategories.InProgress,
        WorkReportStatusCategories.Review,
        WorkReportStatusCategories.Done,
    ];

    private readonly IWorkManagementDbContext _context;

    public WorkReportFactsAdapter(IWorkManagementDbContext context)
    {
        _context = context;
    }

    public async Task<WorkReportFact> ReportAsync(
        Guid workspaceId,
        IReadOnlyCollection<Guid> boardIds,
        DateTimeOffset now,
        int weeks,
        CancellationToken cancellationToken)
    {
        var weekStarts = WeekStarts(now, weeks);
        if (boardIds.Count == 0)
            return Empty(weekStarts);

        var ids = boardIds.ToArray();
        var items = _context.BoardItems
            .AsNoTracking()
            .Where(i => i.WorkspaceId == workspaceId
                && ids.Contains(i.BoardId)
                && i.DeletedAt == null
                && !i.IsArchived);

        var totals = await items
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Open = g.Count(i => i.CompletedAt == null),
                Overdue = g.Count(i => i.CompletedAt == null && i.DueAt != null && i.DueAt < now),
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (totals is null || totals.Total == 0)
            return Empty(weekStarts);

        var byStatus = await StatusAsync(items, ids, totals.Total, cancellationToken);
        var byAssignee = await AssigneesAsync(items, now, cancellationToken);
        var perWeek = await CompletedPerWeekAsync(items, weekStarts, now, cancellationToken);

        var average = await items
            .Where(i => i.CompletedAt != null)
            .GroupBy(_ => 1)
            .Select(g => (double?)g.Average(i => (i.CompletedAt!.Value - i.CreatedAt).TotalSeconds))
            .SingleOrDefaultAsync(cancellationToken);

        return new WorkReportFact(
            totals.Total,
            totals.Open,
            totals.Total - totals.Open,
            totals.Overdue,
            byStatus,
            byAssignee,
            perWeek,
            average);
    }

    public async Task<IReadOnlyList<Guid>> BoardIdsInSpacesAsync(
        Guid workspaceId,
        IReadOnlyCollection<Guid> boardIds,
        IReadOnlyCollection<Guid> spaceIds,
        CancellationToken cancellationToken)
    {
        if (boardIds.Count == 0 || spaceIds.Count == 0)
            return [];

        var boards = boardIds.ToArray();
        var spaces = spaceIds.ToArray();
        return await _context.Boards
            .AsNoTracking()
            .Where(b => b.WorkspaceId == workspaceId
                && b.DeletedAt == null
                && !b.IsArchived
                && boards.Contains(b.Id)
                && b.SpaceId != null
                && spaces.Contains(b.SpaceId.Value))
            .Select(b => b.Id)
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<WorkReportStatusFact>> StatusAsync(
        IQueryable<BoardItem> items,
        Guid[] boardIds,
        int total,
        CancellationToken cancellationToken)
    {
        var fields = await _context.BoardFields
            .AsNoTracking()
            .Include(f => f.Options)
            .Where(f => boardIds.Contains(f.BoardId) && f.DeletedAt == null && f.Type == FieldType.Status)
            .ToListAsync(cancellationToken);

        // Each Board resolves its Status field the way the board read contract does.
        var statusFields = fields
            .GroupBy(f => f.BoardId)
            .Select(g => WorkItemSemanticFields.ResolveStatusField(g))
            .OfType<BoardField>()
            .ToList();
        var fieldIds = statusFields.Select(f => f.Id).ToArray();

        var rows = fieldIds.Length == 0
            ? []
            : await (
                from v in _context.BoardItemValues.AsNoTracking()
                join i in items on v.ItemId equals i.Id
                where fieldIds.Contains(v.FieldId)
                group v by new { v.FieldId, v.Value.Data } into g
                select new { g.Key.FieldId, g.Key.Data, Count = g.Count() })
                .ToListAsync(cancellationToken);

        var optionById = statusFields
            .SelectMany(f => f.Options)
            .ToDictionary(o => o.Id);
        var counted = new Dictionary<Guid, int>();
        foreach (var row in rows)
        {
            var optionId = ReadOptionId(row.Data.Value);
            if (optionId is { } id && optionById.ContainsKey(id))
                counted[id] = counted.GetValueOrDefault(id) + row.Count;
        }

        var none = total - counted.Values.Sum();
        var result = new List<WorkReportStatusFact>();

        if (boardIds.Length == 1 && statusFields.Count == 1)
        {
            result.AddRange(statusFields[0].Options
                .OrderBy(o => o.Position)
                .ThenBy(o => o.Id)
                .Select(o => new WorkReportStatusFact(
                    o.Id, o.Name, o.Color.Value, CategoryOf(o), counted.GetValueOrDefault(o.Id))));
        }
        else
        {
            var byCategory = counted
                .GroupBy(kv => CategoryOf(optionById[kv.Key]))
                .ToDictionary(g => g.Key, g => g.Sum(kv => kv.Value));
            result.AddRange(CategoryOrder.Select(c =>
                new WorkReportStatusFact(null, null, null, c, byCategory.GetValueOrDefault(c))));
        }

        if (none > 0)
            result.Add(new WorkReportStatusFact(null, null, null, WorkReportStatusCategories.None, none));
        return result;
    }

    private async Task<IReadOnlyList<WorkReportAssigneeFact>> AssigneesAsync(
        IQueryable<BoardItem> items,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var assigned = await (
            from m in _context.BoardItemMembers.AsNoTracking()
            join i in items on m.ItemId equals i.Id
            group i by m.UserId into g
            select new
            {
                UserId = g.Key,
                Items = g.Count(),
                Open = g.Count(i => i.CompletedAt == null),
                Overdue = g.Count(i => i.CompletedAt == null && i.DueAt != null && i.DueAt < now),
            })
            .OrderByDescending(x => x.Items)
            .ThenBy(x => x.UserId)
            .Take(MaxAssignees)
            .ToListAsync(cancellationToken);

        var unassigned = await items
            .Where(i => !_context.BoardItemMembers.Any(m => m.ItemId == i.Id))
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Items = g.Count(),
                Open = g.Count(i => i.CompletedAt == null),
                Overdue = g.Count(i => i.CompletedAt == null && i.DueAt != null && i.DueAt < now),
            })
            .SingleOrDefaultAsync(cancellationToken);

        var result = assigned
            .Select(x => new WorkReportAssigneeFact(x.UserId, x.Items, x.Open, x.Overdue))
            .ToList();
        if (unassigned is { Items: > 0 })
            result.Add(new WorkReportAssigneeFact(null, unassigned.Items, unassigned.Open, unassigned.Overdue));
        return result;
    }

    private static async Task<IReadOnlyList<WorkReportWeekFact>> CompletedPerWeekAsync(
        IQueryable<BoardItem> items,
        IReadOnlyList<DateTimeOffset> weekStarts,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var from = weekStarts[0];
        var weekCount = weekStarts.Count;
        var buckets = await items
            .Where(i => i.CompletedAt != null && i.CompletedAt >= from && i.CompletedAt <= now)
            .GroupBy(i => (int)Math.Floor((i.CompletedAt!.Value - from).TotalDays / 7d))
            .Select(g => new { Week = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var byWeek = buckets
            .Where(b => b.Week >= 0 && b.Week < weekCount)
            .ToDictionary(b => b.Week, b => b.Count);
        return weekStarts
            .Select((start, index) => new WorkReportWeekFact(start, byWeek.GetValueOrDefault(index)))
            .ToList();
    }

    /// <summary>Monday 00:00 UTC of each of the last <paramref name="weeks"/> weeks, oldest first.</summary>
    internal static IReadOnlyList<DateTimeOffset> WeekStarts(DateTimeOffset now, int weeks)
    {
        var utc = now.UtcDateTime.Date;
        var sinceMonday = ((int)utc.DayOfWeek + 6) % 7;
        var thisMonday = new DateTimeOffset(utc.AddDays(-sinceMonday), TimeSpan.Zero);
        return Enumerable.Range(0, weeks)
            .Select(i => thisMonday.AddDays(-7 * (weeks - 1 - i)))
            .ToList();
    }

    private static WorkReportFact Empty(IReadOnlyList<DateTimeOffset> weekStarts) => new(
        0, 0, 0, 0,
        CategoryOrder.Select(c => new WorkReportStatusFact(null, null, null, c, 0)).ToList(),
        [],
        weekStarts.Select(w => new WorkReportWeekFact(w, 0)).ToList(),
        null);

    private static string CategoryOf(FieldOption option) =>
        option.Category?.ToString() ?? WorkReportStatusCategories.None;

    private static Guid? ReadOptionId(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.ValueKind == JsonValueKind.String
                && Guid.TryParse(document.RootElement.GetString(), out var id)
                ? id
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
