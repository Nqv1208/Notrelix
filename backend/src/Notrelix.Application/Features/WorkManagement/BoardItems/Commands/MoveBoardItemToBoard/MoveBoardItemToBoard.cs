using System.Text.Json;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.BoardItems.Services;
using Notrelix.Application.Features.WorkManagement.Common;
using Notrelix.Application.Features.WorkManagement.Ordering;
using Notrelix.Application.Features.Governance.Public.Visibility;
using BusinessRuleException = Notrelix.Domain.Common.Exceptions.BusinessRuleException;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.MoveBoardItemToBoard;

/// <summary>
/// Moves a root item (with its sub-items) to another board of the same workspace. The caller needs UpdateItem
/// on the item's board (the request's own permission) and CreateItem on the target board (checked in the
/// handler with the contract creating an item there uses). Status maps by name, else the target's first
/// status; custom values carry over only to a field of the same name and type (option values by option
/// name); labels carry over by name; assignees who cannot view the target board are dropped; whatever is
/// dropped is named in the result. Comments, links and time entries stay with the item.
/// </summary>
[IdempotencyOperation("work-management.board-items.move-board-item-to-board.v1")]
public record MoveBoardItemToBoardCommand(Guid ItemId, Guid TargetBoardId)
    : ICommand<MoveBoardItemToBoardResult>, IWriteRequest, IRequirePermission, IAuthenticatedRequest, IResourceScopedRequest, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.UpdateItem;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board-item"), ItemId);
}

/// <summary>What the move could not carry over, so the person can be told.</summary>
public sealed record MoveBoardItemToBoardResult(
    Guid ItemId,
    Guid BoardId,
    Guid GroupId,
    string ItemKey,
    IReadOnlyList<string> DroppedFields,
    int DroppedLabels,
    int DroppedAssignees);

public sealed class MoveBoardItemToBoardCommandValidator : AbstractValidator<MoveBoardItemToBoardCommand>
{
    public MoveBoardItemToBoardCommandValidator()
    {
        RuleFor(x => x.ItemId).NotEmpty();
        RuleFor(x => x.TargetBoardId).NotEmpty();
    }
}

public class MoveBoardItemToBoardCommandHandler : IRequestHandler<MoveBoardItemToBoardCommand, MoveBoardItemToBoardResult>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _timeProvider;
    private readonly WorkPlacementResolver _placement;
    private readonly IBoardItemKeyAllocator _keys;
    private readonly IBoardItemTargetAuthorizer _targetAuthorizer;
    private readonly IResourceVisibility _visibility;

    public MoveBoardItemToBoardCommandHandler(
        IWorkManagementDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider timeProvider,
        WorkPlacementResolver placement,
        IBoardItemKeyAllocator keys,
        IBoardItemTargetAuthorizer targetAuthorizer,
        IResourceVisibility visibility)
    {
        _context = context;
        _requestContext = requestContext;
        _timeProvider = timeProvider;
        _placement = placement;
        _keys = keys;
        _targetAuthorizer = targetAuthorizer;
        _visibility = visibility;
    }

    public async Task<MoveBoardItemToBoardResult> Handle(MoveBoardItemToBoardCommand request, CancellationToken ct)
    {
        // The values are not loaded with the items: they are read and deleted below, so the move's own clearing
        // of the item's values has nothing tracked to orphan.
        var item = await _context.BoardItems
            .FirstOrDefaultAsync(i => i.Id == request.ItemId && i.DeletedAt == null, ct);
        if (item is null) throw new NotFoundException(nameof(BoardItem), request.ItemId);

        if (item.ParentItemId is not null)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Item_SubItemFollowsParentGroup, "A sub-item moves with its parent.");
        if (item.IsArchived)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Item_CannotModifyArchived, "Restore the item before moving it to another board.");
        if (item.BoardId == request.TargetBoardId)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_BoardScopeMismatch, "The item is already on this board.");

        await _targetAuthorizer.AuthorizeCreateAsync(request.TargetBoardId, ct);

        var sourceBoardId = item.BoardId;
        var userId = _requestContext.UserId;
        var now = _timeProvider.UtcNow;

        var dependencyHeld = await _context.ItemDependencies
            .AnyAsync(d => (d.PredecessorItemId == item.Id || d.SuccessorItemId == item.Id) && d.DeletedAt == null, ct);
        if (dependencyHeld)
            throw new ConflictException("Remove the item's dependencies before moving it to another board.", "work-management.item.has-dependencies");

        var targetBoard = await _context.Boards.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == request.TargetBoardId && b.DeletedAt == null, ct);
        if (targetBoard is null || targetBoard.WorkspaceId != item.WorkspaceId)
            throw new NotFoundException(nameof(Board), request.TargetBoardId);
        if (targetBoard.IsArchived)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Board_CannotGenerateIdentityArchived, "Cannot move an item to an archived board.");

        // The target group: the one with the source group's title, else the board's default, else its first.
        var sourceGroup = await _context.BoardGroups.AsNoTracking().FirstOrDefaultAsync(g => g.Id == item.GroupId, ct);
        var targetGroups = await _context.BoardGroups.AsNoTracking()
            .Where(g => g.BoardId == targetBoard.Id && !g.IsArchived)
            .ToListAsync(ct);
        var targetGroup =
            targetGroups.FirstOrDefault(g => sourceGroup is not null && string.Equals(g.Title, sourceGroup.Title, StringComparison.OrdinalIgnoreCase))
            ?? targetGroups.FirstOrDefault(g => g.Id == targetBoard.DefaultItemGroupId)
            ?? targetGroups.OrderBy(g => g.Position.Value, StringComparer.Ordinal).FirstOrDefault()
            ?? throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Board_CannotGenerateIdentityArchived, "The target board has no group to receive the item.");
        var targetRef = BoardGroupRef.From(targetGroup);

        var sourceFields = await _context.BoardFields.AsNoTracking().Include(f => f.Options)
            .Where(f => f.BoardId == sourceBoardId && f.DeletedAt == null).ToListAsync(ct);
        var targetFields = await _context.BoardFields.Include(f => f.Options)
            .Where(f => f.BoardId == targetBoard.Id && f.DeletedAt == null).ToListAsync(ct);
        var sourceStatus = WorkItemSemanticFields.ResolveStatusField(sourceFields);
        var targetStatus = WorkItemSemanticFields.ResolveStatusField(targetFields);

        var children = await _context.BoardItems
            .Where(c => c.ParentItemId == item.Id && c.DeletedAt == null)
            .ToListAsync(ct);
        var moving = new List<BoardItem> { item };
        moving.AddRange(children);
        var movingIds = moving.Select(i => i.Id).ToList();

        var held = await _context.BoardItemValues.AsNoTracking()
            .Where(v => movingIds.Contains(v.ItemId))
            .Select(v => new { v.ItemId, v.FieldId, v.Value })
            .ToListAsync(ct);
        var oldValues = moving.ToDictionary(
            i => i.Id,
            i => held.Where(v => v.ItemId == i.Id).Select(v => (v.FieldId, v.Value)).ToList());
        // Deleted now, while the items are still on their board: a value row is only visible through the board
        // its field belongs to, so a delete the save orders after the items' move would match no row.
        await _context.BoardItemValues.Where(v => movingIds.Contains(v.ItemId)).ExecuteDeleteAsync(ct);

        var position = await _placement.ResolveAsync(
            new WorkOrderingScope(WorkOrderingScopeKind.GroupItems, targetGroup.Id),
            new WorkPlacementIntent(null, null),
            token => GroupItemSiblings.LoadAsync(_context, targetGroup.Id, token),
            ct);

        // Keys are reserved last, after the ordering lock, as creating an item does.
        var keys = await _keys.AllocateAsync(_context, targetBoard.Id, moving.Count, ct);
        item.MoveToBoard(targetRef, position, keys[0].Key, keys[0].Sequence, userId, now);
        for (var i = 0; i < children.Count; i++)
            children[i].FollowParentToBoard(targetRef, keys[i + 1].Key, keys[i + 1].Sequence, userId, now);

        // The items' own move is written before their new values: a value row is only visible through the board of
        // its field, so the insert the save would otherwise order first is refused for an item still on its old board.
        foreach (var moved in moving)
        {
            var (boardId, groupId, itemKey, sequence, at) = (moved.BoardId, moved.GroupId, moved.ItemKey, moved.ItemSequence, moved.Position);
            await _context.BoardItems.Where(i => i.Id == moved.Id).ExecuteUpdateAsync(
                set => set
                    .SetProperty(i => i.BoardId, boardId)
                    .SetProperty(i => i.GroupId, groupId)
                    .SetProperty(i => i.ItemKey, itemKey)
                    .SetProperty(i => i.ItemSequence, sequence)
                    .SetProperty(i => i.Position, at),
                ct);
        }

        var dropped = new List<string>();
        foreach (var moved in moving)
            CarryValues(moved, oldValues[moved.Id], sourceFields, sourceStatus, targetFields, targetStatus, userId, now, dropped);

        // Assignees stay when they can view the target board (a move never widens who sees an item).
        var members = await _context.BoardItemMembers.Where(m => movingIds.Contains(m.ItemId)).ToListAsync(ct);
        var droppedAssignees = 0;
        if (members.Count > 0)
        {
            var visibleByUser = new Dictionary<Guid, bool>();
            foreach (var userIdGroup in members.Select(m => m.UserId).Distinct())
            {
                var visible = await _visibility.VisibleBoardIdsAsync(item.WorkspaceId, userIdGroup, ct);
                visibleByUser[userIdGroup] = visible.Contains(targetBoard.Id);
            }
            foreach (var member in members)
            {
                if (visibleByUser[member.UserId]) member.FollowItemToBoard(targetBoard.Id);
                else { _context.BoardItemMembers.Remove(member); droppedAssignees++; }
            }
        }

        // Labels belong to a board: kept by name, else dropped.
        var sourceLabels = await _context.BoardItemLabels.Where(l => movingIds.Contains(l.ItemId)).ToListAsync(ct);
        var droppedLabels = 0;
        if (sourceLabels.Count > 0)
        {
            var labelIds = sourceLabels.Select(l => l.LabelId).Distinct().ToList();
            var names = await _context.Labels.AsNoTracking().Where(l => labelIds.Contains(l.Id)).ToDictionaryAsync(l => l.Id, l => l.Name, ct);
            var targetLabels = await _context.Labels.AsNoTracking()
                .Where(l => l.BoardId == targetBoard.Id && l.DeletedAt == null && l.Status == LabelStatus.Active).ToListAsync(ct);
            foreach (var label in sourceLabels)
            {
                _context.BoardItemLabels.Remove(label);
                var match = names.TryGetValue(label.LabelId, out var name)
                    ? targetLabels.FirstOrDefault(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase))
                    : null;
                if (match is null) { droppedLabels++; continue; }
                _context.BoardItemLabels.Add(BoardItemLabel.Create(
                    item.AccountId, item.WorkspaceId, targetBoard.Id, label.ItemId, match.Id, label.CreatedBy, now));
            }
        }

        foreach (var link in await _context.BoardItemLinks.Where(l => movingIds.Contains(l.SourceItemId)).ToListAsync(ct))
            link.FollowItemToBoard(targetBoard.Id);
        foreach (var entry in await _context.TimeTrackingEntries.Where(t => movingIds.Contains(t.ItemId)).ToListAsync(ct))
            entry.FollowItemToBoard(targetBoard.Id);

        return new MoveBoardItemToBoardResult(
            item.Id, targetBoard.Id, targetGroup.Id, keys[0].Key,
            dropped.Distinct(StringComparer.OrdinalIgnoreCase).ToList(), droppedLabels, droppedAssignees);
    }

    private static void CarryValues(
        BoardItem moved,
        List<(Guid FieldId, FieldValue Value)> old,
        List<BoardField> sourceFields,
        BoardField? sourceStatus,
        List<BoardField> targetFields,
        BoardField? targetStatus,
        Guid userId,
        DateTimeOffset now,
        List<string> dropped)
    {
        if (targetStatus is not null)
        {
            var oldOptionId = sourceStatus is null
                ? null
                : WorkItemSemanticFields.ReadOptionId(old.FirstOrDefault(v => v.FieldId == sourceStatus.Id).Value);
            var oldName = sourceStatus?.Options.FirstOrDefault(o => o.Id == oldOptionId)?.Name;
            var option = (oldName is null ? null : targetStatus.Options.FirstOrDefault(o => string.Equals(o.Name, oldName, StringComparison.OrdinalIgnoreCase)))
                ?? targetStatus.Options.OrderBy(o => o.Position.Value, StringComparer.Ordinal).FirstOrDefault();
            if (option is not null)
                Write(moved, targetStatus, OptionValue(option.Id), targetStatus, userId, now, dropped, targetStatus.Name);
        }

        foreach (var (fieldId, value) in old)
        {
            var source = sourceFields.FirstOrDefault(f => f.Id == fieldId);
            if (source is null || source.IsSystem || source.Id == sourceStatus?.Id || value.IsEmpty) continue;
            var target = targetFields.FirstOrDefault(f =>
                !f.IsSystem && f.Id != targetStatus?.Id && f.Type == source.Type
                && string.Equals(f.Name, source.Name, StringComparison.OrdinalIgnoreCase));
            if (target is null) { dropped.Add(source.Name); continue; }

            var mapped = Map(value, source, target);
            if (mapped is null) { dropped.Add(source.Name); continue; }
            Write(moved, target, mapped, targetStatus, userId, now, dropped, source.Name);
        }
    }

    private static void Write(BoardItem item, BoardField field, FieldValue value, BoardField? statusField, Guid userId, DateTimeOffset now, List<string> dropped, string name)
    {
        try { BoardItemFieldWrites.Write(item, field, value, statusField, userId, now); }
        catch (BusinessRuleException) { dropped.Add(name); }
    }

    private static FieldValue OptionValue(Guid optionId) =>
        FieldValue.Create(JsonValue.Create(JsonSerializer.Serialize(optionId.ToString())));

    /// <summary>The value on the target field, or null when it cannot carry over.</summary>
    private static FieldValue? Map(FieldValue value, BoardField source, BoardField target)
    {
        switch (source.Type)
        {
            case FieldType.Select:
                {
                    var optionId = WorkItemSemanticFields.ReadOptionId(value);
                    var name = source.Options.FirstOrDefault(o => o.Id == optionId)?.Name;
                    var match = name is null ? null : target.Options.FirstOrDefault(o => string.Equals(o.Name, name, StringComparison.OrdinalIgnoreCase));
                    return match is null ? null : OptionValue(match.Id);
                }
            case FieldType.MultiSelect:
                {
                    try
                    {
                        using var document = JsonDocument.Parse(value.Data.Value);
                        if (document.RootElement.ValueKind != JsonValueKind.Array) return null;
                        var ids = new List<string>();
                        foreach (var element in document.RootElement.EnumerateArray())
                        {
                            if (element.ValueKind != JsonValueKind.String || !Guid.TryParse(element.GetString(), out var id)) continue;
                            var name = source.Options.FirstOrDefault(o => o.Id == id)?.Name;
                            var match = name is null ? null : target.Options.FirstOrDefault(o => string.Equals(o.Name, name, StringComparison.OrdinalIgnoreCase));
                            if (match is not null) ids.Add(match.Id.ToString());
                        }
                        return ids.Count == 0 ? null : FieldValue.Create(JsonValue.Create(JsonSerializer.Serialize(ids)));
                    }
                    catch (JsonException)
                    {
                        return null;
                    }
                }
            default:
                return value;
        }
    }
}
