using System.Text.Json;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Templates.Services;

/// <summary>
/// Reads a Board's structure (fields with options, groups, labels, board views, and optionally root items with
/// their values and, when asked, assignees) into the portable <see cref="BoardTemplateStructure"/>. One reader for
/// saving a template and duplicating a board, so the two copy exactly the same things.
/// </summary>
public static class BoardStructureCapture
{
    public static async Task<BoardTemplateStructure> CaptureAsync(
        IWorkManagementDbContext context,
        Board board,
        bool includeItems,
        bool includeAssignees,
        Func<int, Exception> tooManyItems,
        CancellationToken ct)
    {
        var fields = await context.BoardFields.AsNoTracking()
            .Where(f => f.BoardId == board.Id && f.Type != FieldType.Rollup)
            .ToListAsync(ct);
        var fieldIds = fields.Select(f => f.Id).ToList();
        var options = await context.FieldOptions.AsNoTracking()
            .Where(o => fieldIds.Contains(o.FieldId))
            .ToListAsync(ct);
        var groups = await context.BoardGroups.AsNoTracking()
            .Where(g => g.BoardId == board.Id && !g.IsArchived)
            .ToListAsync(ct);
        var labels = await context.Labels.AsNoTracking()
            .Where(l => l.BoardId == board.Id && l.Status == LabelStatus.Active)
            .ToListAsync(ct);
        var views = await context.BoardViews.AsNoTracking()
            .Where(v => v.BoardId == board.Id && !v.IsArchived && v.Visibility == BoardViewVisibility.Board && v.Type != ViewType.Form)
            .ToListAsync(ct);

        return new BoardTemplateStructure(
            BoardTemplateStructure.CurrentVersion,
            fields.OrderBy(f => f.Position.Value, StringComparer.Ordinal).Select(f => new TemplateField(
                f.Id, f.Name, f.Type.ToString(), f.IsSystem, Raw(f.Settings.Data.Value),
                options.Where(o => o.FieldId == f.Id).OrderBy(o => o.Position.Value, StringComparer.Ordinal)
                    .Select(o => new TemplateOption(o.Id, o.Name, o.Color.Value, o.Category?.ToString())).ToList())).ToList(),
            groups.OrderBy(g => g.Position.Value, StringComparer.Ordinal).Select(g => new TemplateGroup(g.Id, g.Title, g.Color.Value)).ToList(),
            labels.Select(l => new TemplateLabel(l.Name, l.Color.Hex)).ToList(),
            views.Select(v => new TemplateView(v.Name, v.Type.ToString(), v.IsDefault, Raw(v.Config.Data.Value))).ToList(),
            includeItems ? await CaptureItemsAsync(context, board.Id, groups.Select(g => g.Id).ToHashSet(), fieldIds, includeAssignees, tooManyItems, ct) : []);
    }

    private static async Task<List<TemplateItem>> CaptureItemsAsync(
        IWorkManagementDbContext context, Guid boardId, HashSet<Guid> groupIds, List<Guid> fieldIds, bool includeAssignees, Func<int, Exception> tooManyItems, CancellationToken ct)
    {
        var items = await context.BoardItems.AsNoTracking()
            .Where(i => i.BoardId == boardId && i.ParentItemId == null && !i.IsArchived)
            .ToListAsync(ct);
        var kept = items.Where(i => groupIds.Contains(i.GroupId))
            .OrderBy(i => i.Position.Value, StringComparer.Ordinal)
            .ToList();
        // Never a silently shortened board: past the cap the caller's refusal is thrown.
        if (kept.Count > BoardTemplateStructure.MaxItems) throw tooManyItems(kept.Count);
        var itemIds = kept.Select(i => i.Id).ToList();
        var values = await context.BoardItemValues.AsNoTracking()
            .Where(v => itemIds.Contains(v.ItemId) && fieldIds.Contains(v.FieldId))
            .ToListAsync(ct);

        var assignees = includeAssignees
            ? (await context.BoardItemMembers.AsNoTracking().Where(m => itemIds.Contains(m.ItemId)).Select(m => new { m.ItemId, m.UserId }).ToListAsync(ct))
                .GroupBy(m => m.ItemId).ToDictionary(g => g.Key, g => g.Select(m => m.UserId).Distinct().ToList())
            : [];

        return kept.Select(i => new TemplateItem(
            i.GroupId, i.Name, i.DescriptionMd,
            values.Where(v => v.ItemId == i.Id).ToDictionary(v => v.FieldId, v => Raw(v.Value.Data.Value)),
            assignees.GetValueOrDefault(i.Id))).ToList();
    }

    private static JsonElement Raw(string json) => JsonDocument.Parse(json).RootElement.Clone();
}
