using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Common.DTOs;
using Notrelix.Application.Features.WorkManagement.Ordering;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Services;

/// <summary>
/// Producer-local move-item use case shared by the HTTP command handler and
/// the WorkManagement Public target action. This is the single mutation
/// implementation for moving a board item between groups on the same board;
/// callers only differ in how the execution principal/scope arrives.
/// </summary>
public sealed class MoveBoardItemUseCase
{
    private readonly IWorkManagementDbContext _context;
    private readonly IDateTimeProvider _timeProvider;
    private readonly WorkPlacementResolver _placement;

    public MoveBoardItemUseCase(IWorkManagementDbContext context, IDateTimeProvider timeProvider, WorkPlacementResolver placement)
    {
        _context = context;
        _timeProvider = timeProvider;
        _placement = placement;
    }

    public async Task<MoveBoardItemOutcome> MoveAsync(
        Guid itemId,
        Guid newGroupId,
        WorkPlacementIntent placement,
        Guid executorUserId,
        CancellationToken cancellationToken,
        LaneValueWrite? lane = null)
    {
        // The lane's value is written on the same item in the same transaction: its values are loaded for that.
        IQueryable<BoardItem> items = _context.BoardItems;
        if (lane is not null) items = items.Include(i => i.FieldValues);
        var item = await items
            .FirstOrDefaultAsync(item => item.Id == itemId, cancellationToken);

        if (item == null)
            throw new NotFoundException("BoardItem", itemId);

        // A sub-item lives in its parent's group and only moves with the parent.
        if (item.ParentItemId is not null)
            throw new Notrelix.Domain.Common.Exceptions.BusinessRuleException(
                WorkManagementRuleCodes.WorkManagement_Item_SubItemFollowsParentGroup,
                "A sub-item stays in its parent's group; move the parent instead.");

        var group = await _context.BoardGroups
            .FirstOrDefaultAsync(g => g.Id == newGroupId && g.BoardId == item.BoardId, cancellationToken);

        if (group == null)
            throw new NotFoundException("BoardGroup", newGroupId);

        var now = _timeProvider.UtcNow;
        var position = await _placement.ResolveAsync(
            new WorkOrderingScope(WorkOrderingScopeKind.GroupItems, group.Id),
            placement,
            ct => GroupItemSiblings.LoadAsync(_context, group.Id, ct),
            cancellationToken,
            movedEntityId: item.Id);

        var groupBefore = item.GroupId;
        item.MoveToGroup(BoardGroupRef.From(group), position, executorUserId, now);

        if (lane is not null)
            await WriteLaneValueAsync(item, lane, executorUserId, now, cancellationToken);

        // Its sub-items follow it, keeping their place under it.
        if (groupBefore != group.Id)
        {
            var children = await _context.BoardItems
                .Where(child => child.ParentItemId == item.Id && child.DeletedAt == null)
                .ToListAsync(cancellationToken);
            foreach (var child in children)
                child.MoveToGroupOnGroupRemoval(BoardGroupRef.From(group), null, executorUserId, now);
        }

        var memberIds = await _context.BoardItemMembers
            .Where(m => m.ItemId == item.Id)
            .Select(m => m.UserId)
            .ToListAsync(cancellationToken);

        var labelIds = await _context.BoardItemLabels
            .Where(l => l.ItemId == item.Id)
            .Select(l => l.LabelId)
            .ToListAsync(cancellationToken);

        return new MoveBoardItemOutcome(
            new BoardItemSlimDto(
                item.Id,
                item.GroupId,
                item.Name,
                item.Position.Value,
                memberIds,
                labelIds,
                item.Version),
            item.Version);
    }

    // A card dropped in a lane takes the lane's option and the place in one change: Status through the
    // status rules (completion follows the option's category), any other Select as an ordinary value.
    private async Task WriteLaneValueAsync(BoardItem item, LaneValueWrite lane, Guid executorUserId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var field = await _context.BoardFields
            .Include(f => f.Options)
            .FirstOrDefaultAsync(f => f.Id == lane.FieldId && f.BoardId == item.BoardId, cancellationToken);
        if (field == null)
            throw new NotFoundException("BoardField", lane.FieldId);
        if (field.Type is not (FieldType.Select or FieldType.Status))
            throw new Notrelix.Domain.Common.Exceptions.BusinessRuleException(
                WorkManagementRuleCodes.WorkManagement_Field_InvalidOptionValue,
                $"The field '{field.Name}' has no options to move an item into.");

        var value = FieldValue.Create(JsonValue.Create(System.Text.Json.JsonSerializer.Serialize(lane.OptionId.ToString())));
        var statusField = await BoardItemFieldWrites.LoadStatusFieldAsync(_context, item.BoardId, cancellationToken);
        BoardItemFieldWrites.Write(item, field, value, statusField, executorUserId, now);
    }
}

/// <summary>The option a card takes in a board field when it is dropped in that field's lane.</summary>
public sealed record LaneValueWrite(Guid FieldId, Guid OptionId);

/// <summary>
/// Producer-internal move outcome: the shared slim response plus the aggregate
/// version after the move (realtime stream consumers need the stream version).
/// </summary>
public sealed record MoveBoardItemOutcome(BoardItemSlimDto Item, long Version);
