using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Common;
using Notrelix.Application.Features.WorkManagement.Common.DTOs;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Services;

/// <summary>Item values the board read contract surfaces next to an Item.</summary>
public sealed record BoardItemReadValue(Guid? StatusOptionId, Guid? PriorityOptionId, BoardItemChildProgressDto? ChildProgress);

/// <summary>
/// Loads, set-based, the read values of a set of Items of one Board: the option
/// held by the Board's Status field and by its priority field
/// (<see cref="WorkItemSemanticFields"/>), and the derived child progress — the
/// Item's non-archived child Items and how many of them hold a Status option of the
/// done category. Two queries whatever the number of Items: the child rows of all
/// requested parents, then the Status/priority values of the parents and children.
/// </summary>
public static class BoardItemReadValues
{
    public static async Task<IReadOnlyDictionary<Guid, BoardItemReadValue>> LoadAsync(
        IWorkManagementDbContext context,
        IReadOnlyCollection<BoardField> boardFields,
        IReadOnlyCollection<Guid> itemIds,
        CancellationToken cancellationToken)
    {
        if (itemIds.Count == 0) return new Dictionary<Guid, BoardItemReadValue>();

        var status = WorkItemSemanticFields.ResolveStatusField(boardFields);
        var priority = WorkItemSemanticFields.ResolvePriorityField(boardFields);

        var children = await context.BoardItems
            .AsNoTracking()
            .Where(child => child.ParentItemId != null && itemIds.Contains(child.ParentItemId.Value) && !child.IsArchived)
            .Select(child => new { child.Id, ParentId = child.ParentItemId!.Value })
            .ToListAsync(cancellationToken);

        var fieldIds = new[] { status?.Id, priority?.Id }.OfType<Guid>().ToList();
        var valueItemIds = itemIds.Concat(children.Select(child => child.Id)).Distinct().ToList();
        var values = fieldIds.Count == 0
            ? []
            : await context.BoardItemValues
                .AsNoTracking()
                .Where(value => valueItemIds.Contains(value.ItemId) && fieldIds.Contains(value.FieldId))
                .ToListAsync(cancellationToken);

        // One value per (item, field): the table is unique on that pair.
        var valueByItemField = values.ToDictionary(value => (value.ItemId, value.FieldId), value => value.Value);

        Guid? OptionOf(Guid itemId, BoardField? field) =>
            field is not null && valueByItemField.TryGetValue((itemId, field.Id), out var value)
                ? WorkItemSemanticFields.ReadOptionId(value)
                : null;

        var doneOptionIds = status?.Options
            .Where(option => option.Category == WorkStatusCategory.Done)
            .Select(option => option.Id)
            .ToHashSet() ?? [];
        var childrenByParent = children.GroupBy(child => child.ParentId).ToDictionary(group => group.Key, group => group.Select(child => child.Id).ToList());

        var result = new Dictionary<Guid, BoardItemReadValue>(itemIds.Count);
        foreach (var itemId in itemIds)
        {
            BoardItemChildProgressDto? progress = null;
            if (childrenByParent.TryGetValue(itemId, out var childIds))
            {
                var done = childIds.Count(childId => OptionOf(childId, status) is { } option && doneOptionIds.Contains(option));
                progress = new BoardItemChildProgressDto(done, childIds.Count);
            }

            result[itemId] = new BoardItemReadValue(OptionOf(itemId, status), OptionOf(itemId, priority), progress);
        }

        return result;
    }
}
