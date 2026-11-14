using System.Text.Json;
using Notrelix.Application.Features.Governance.Public.Visibility;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Common.DTOs;

namespace Notrelix.Application.Features.WorkManagement.RelationTargets;

/// <summary>
/// The read side of a Relation field. A stored link resolves to a title only when the viewer can read the target
/// board; otherwise it is a locked link with no title (D78.6: "Không có quyền xem"). A link whose target item is in
/// the trash, was permanently deleted, or no longer sits on the field's target board is hidden, not shown as a
/// broken chip, and comes back when the item is restored (product section 67: the link is not retargeted).
/// Set-based: one query for the targets of every item of the board.
/// </summary>
public static class BoardItemRelationLinks
{
    /// <summary>The links of every Relation field, by item id then field id (string).</summary>
    public static async Task<IReadOnlyDictionary<Guid, IReadOnlyDictionary<string, List<RelationLinkDto>>>> LoadAsync(
        IWorkManagementDbContext context,
        IResourceVisibility? visibility,
        Guid viewerUserId,
        IReadOnlyCollection<BoardField> fields,
        IReadOnlyDictionary<Guid, IReadOnlyDictionary<string, JsonElement>> storedByItem,
        CancellationToken cancellationToken)
    {
        var relationFields = fields
            .Where(f => f.Type == FieldType.Relation && f.DeletedAt == null)
            .Select(f => (Field: f, Target: RelationFieldTargets.TargetBoardIdOf(f)))
            .Where(f => f.Target is not null)
            .ToList();
        var byItem = new Dictionary<Guid, Dictionary<string, List<RelationLinkDto>>>();
        if (relationFields.Count == 0 || storedByItem.Count == 0) return new Dictionary<Guid, IReadOnlyDictionary<string, List<RelationLinkDto>>>();

        var workspaceId = relationFields[0].Field.WorkspaceId;
        var readable = visibility is null
            ? []
            : (await visibility.VisibleBoardIdsAsync(workspaceId, viewerUserId, cancellationToken)).ToHashSet();

        // Stored ids per item and field, and every id of a field whose target the viewer can read.
        var storedIds = new Dictionary<(Guid Item, Guid Field), List<Guid>>();
        var readableIds = new HashSet<Guid>();
        foreach (var (itemId, stored) in storedByItem)
        {
            foreach (var (field, target) in relationFields)
            {
                if (!stored.TryGetValue(field.Id.ToString(), out var json)) continue;
                var ids = ParseIds(json);
                storedIds[(itemId, field.Id)] = ids;
                if (target is Guid t && readable.Contains(t)) readableIds.UnionWith(ids);
            }
        }

        var targets = readableIds.Count == 0
            ? new Dictionary<Guid, TargetRow>()
            : await context.BoardItems.AsNoTracking()
                .Where(item => readableIds.Contains(item.Id))
                .Select(item => new TargetRow(item.Id, item.BoardId, item.ItemKey, item.Name, item.DeletedAt != null))
                .ToDictionaryAsync(row => row.Id, cancellationToken);

        foreach (var ((itemId, fieldId), ids) in storedIds)
        {
            var (field, target) = relationFields.First(f => f.Field.Id == fieldId);
            var links = new List<RelationLinkDto>();
            foreach (var id in ids)
            {
                if (target is not Guid targetBoardId) continue;
                if (!readable.Contains(targetBoardId))
                {
                    links.Add(new RelationLinkDto(id, null, null, Locked: true));
                    continue;
                }
                if (!targets.TryGetValue(id, out var row) || row.Deleted || row.BoardId != targetBoardId) continue;
                links.Add(new RelationLinkDto(id, row.ItemKey, row.Name, Locked: false));
            }

            if (!byItem.TryGetValue(itemId, out var byField))
            {
                byField = new Dictionary<string, List<RelationLinkDto>>();
                byItem[itemId] = byField;
            }
            byField[field.Id.ToString()] = links;
        }

        return byItem.ToDictionary(pair => pair.Key, pair => (IReadOnlyDictionary<string, List<RelationLinkDto>>)pair.Value);
    }

    /// <summary>The stored values with each Relation field holding the ids of the links the viewer is shown (titles never travel here).</summary>
    public static IReadOnlyDictionary<string, JsonElement> WithLinkIds(
        IReadOnlyDictionary<string, JsonElement> stored,
        IReadOnlyDictionary<string, List<RelationLinkDto>>? links)
    {
        if (links is null || links.Count == 0) return stored;
        var merged = new Dictionary<string, JsonElement>(stored);
        foreach (var (fieldId, shown) in links)
            merged[fieldId] = JsonSerializer.SerializeToElement(shown.Select(link => link.ItemId).ToList());
        return merged;
    }

    private static List<Guid> ParseIds(JsonElement json) =>
        json.ValueKind == JsonValueKind.Array
            ? json.EnumerateArray()
                .Where(e => e.ValueKind == JsonValueKind.String && Guid.TryParse(e.GetString(), out _))
                .Select(e => Guid.Parse(e.GetString()!))
                .ToList()
            : [];

    private sealed record TargetRow(Guid Id, Guid BoardId, string? ItemKey, string Name, bool Deleted);
}
