using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.BoardGroups.Commands.DuplicateBoardGroup;
using Notrelix.Application.Features.WorkManagement.BoardItems.Services;
using Notrelix.Application.Features.WorkManagement.Common;
using Notrelix.Application.Features.WorkManagement.Ordering;
using BusinessRuleException = Notrelix.Domain.Common.Exceptions.BusinessRuleException;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.DuplicateBoardItem;

[IdempotencyOperation("work-management.board-items.duplicate-board-item.v1")]
public record DuplicateBoardItemCommand(Guid BoardItemId, string? Name = null, DuplicateBoardItemOptions? Options = null) : ICommand<Result<Guid>>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.CreateItem;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board-item"), BoardItemId);
}

public class DuplicateBoardItemCommandHandler : IRequestHandler<DuplicateBoardItemCommand, Result<Guid>>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _timeProvider;
    private readonly WorkPlacementResolver _placement;
    private readonly IBoardItemKeyAllocator _keys;
    private readonly IBoardRealtimeNotifier? _realtime;

    public DuplicateBoardItemCommandHandler(IWorkManagementDbContext context, ICurrentRequestContext requestContext, IDateTimeProvider timeProvider, WorkPlacementResolver placement, IBoardItemKeyAllocator keys, IBoardRealtimeNotifier? realtime = null)
    {
        _realtime = realtime;
        _keys = keys;
        _context = context;
        _requestContext = requestContext;
        _timeProvider = timeProvider;
        _placement = placement;
    }

    public async Task<Result<Guid>> Handle(DuplicateBoardItemCommand request, CancellationToken ct)
    {
        var response = await HandleCore(request, ct);
        if (_realtime is not null) await _realtime.NotifyAsync(request, response, ct);
        return response;
    }

    private async Task<Result<Guid>> HandleCore(DuplicateBoardItemCommand request, CancellationToken ct)
    {
        var options = request.Options ?? new DuplicateBoardItemOptions();
        var source = await _context.BoardItems
            .Include(i => i.FieldValues)
            .FirstOrDefaultAsync(c => c.Id == request.BoardItemId && c.DeletedAt == null, ct);
        if (source is null) throw new NotFoundException(nameof(BoardItem), request.BoardItemId);

        // A sub-item is copied with its parent, never alone: its parent path would point at the original parent.
        if (source.ParentItemId is not null)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Item_SubItemFollowsParentGroup, "Duplicate the parent item to copy a sub-item.");

        // The copy goes directly after its source, under the group's ordering lock (WMREQ049).
        var nextPosition = await _placement.ResolveAfterAsync(
            new WorkOrderingScope(WorkOrderingScopeKind.GroupItems, source.GroupId),
            source.Id,
            token => GroupItemSiblings.LoadAsync(_context, source.GroupId, token),
            ct);

        var now = _timeProvider.UtcNow;
        var userId = _requestContext.UserId;
        var accountId = _requestContext.RequireAccountId();

        var children = options.IncludeSubItems
            ? (await _context.BoardItems.Include(i => i.FieldValues)
                .Where(i => i.ParentItemId == source.Id && i.DeletedAt == null).ToListAsync(ct))
                .OrderBy(i => i.Position.Value, StringComparer.Ordinal).ToList()
            : [];

        // Keys are reserved after the ordering lock: the copy first, then its sub-items in order.
        var keys = await _keys.AllocateAsync(_context, source.BoardId, 1 + children.Count, ct);

        var name = string.IsNullOrWhiteSpace(request.Name) ? $"{source.Name} copy" : request.Name.Trim();
        var duplicate = DuplicateBoardGroupCommandHandler.CloneCard(
            source, accountId, source.GroupId, source.BoardId, source.WorkspaceId, userId, name, nextPosition, now, keys[0]);
        _context.BoardItems.Add(duplicate);

        BoardField? statusField = null;
        List<BoardField> fields = [];
        if (options.IncludeFields)
        {
            fields = await _context.BoardFields.Include(f => f.Options)
                .Where(f => f.BoardId == source.BoardId && f.DeletedAt == null).ToListAsync(ct);
            statusField = WorkItemSemanticFields.ResolveStatusField(fields);
            CopyValues(source, duplicate, fields, statusField, userId, now);
        }

        for (var i = 0; i < children.Count; i++)
        {
            var child = children[i];
            var copy = BoardItem.CreateSubItem(duplicate, child.Name, child.Position, userId, now, keys[i + 1].Key, keys[i + 1].Sequence);
            _context.BoardItems.Add(copy);
            if (options.IncludeFields) CopyValues(child, copy, fields, statusField, userId, now);
        }

        if (options.IncludeChecklists)
        {
            var checklists = await _context.Checklists
                .Where(c => c.ItemId == source.Id && c.DeletedAt == null).ToListAsync(ct);
            foreach (var checklist in checklists.OrderBy(c => c.Position.Value, StringComparer.Ordinal))
            {
                var copy = Checklist.Create(accountId, source.WorkspaceId, duplicate.Id, checklist.Title, checklist.Position, userId, now);
                foreach (var entry in checklist.Items.OrderBy(e => e.Position.Value, StringComparer.Ordinal))
                    copy.AddItem(entry.Title, entry.Position, userId, now);
                _context.Checklists.Add(copy);
            }
        }

        if (options.IncludeLabels)
        {
            foreach (var label in await _context.BoardItemLabels.Where(l => l.ItemId == source.Id).ToListAsync(ct))
                _context.BoardItemLabels.Add(BoardItemLabel.Create(accountId, source.WorkspaceId, source.BoardId, duplicate.Id, label.LabelId, userId, now));
        }

        if (options.IncludeAssignees)
        {
            foreach (var member in await _context.BoardItemMembers.Where(m => m.ItemId == source.Id).ToListAsync(ct))
                _context.BoardItemMembers.Add(BoardItemMember.Create(accountId, source.WorkspaceId, source.BoardId, duplicate.Id, member.UserId, userId, now));
        }

        return Result<Guid>.Success(duplicate.Id);
    }

    /// <summary>
    /// Copies the values of the board's own fields; system fields other than Status mirror other facts (the
    /// assignees, the dates) and are not values to copy. A value the field no longer accepts is skipped.
    /// </summary>
    private static void CopyValues(BoardItem from, BoardItem to, List<BoardField> fields, BoardField? statusField, Guid userId, DateTimeOffset now)
    {
        foreach (var value in from.FieldValues.ToList())
        {
            var field = fields.FirstOrDefault(f => f.Id == value.FieldId);
            if (field is null || value.Value.IsEmpty) continue;
            if (field.IsSystem && field.Id != statusField?.Id) continue;
            try { BoardItemFieldWrites.Write(to, field, value.Value, statusField, userId, now); }
            catch (BusinessRuleException) { }
        }
    }
}
