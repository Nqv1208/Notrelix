using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.ArchiveBoardItem;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.AssignBoardItemMember;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.DeleteBoardItem;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UpdateBoardItemFieldValue;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UpdateBoardItemStatus;
using Notrelix.Application.Features.WorkManagement.Common;
using Notrelix.Application.Features.WorkManagement.Common.DTOs;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.BulkUpdateBoardItems;

/// <summary>The one change a bulk request applies to every item it names.</summary>
public static class BulkBoardItemOperations
{
    public const string SetStatus = "set-status";
    public const string SetDueDate = "set-due-date";
    public const string ClearDueDate = "clear-due-date";
    public const string Assign = "assign";
    public const string UnassignAll = "unassign-all";
    public const string Archive = "archive";
    public const string SetPriority = "set-priority";
    public const string Delete = "delete";

    public const int MaxItems = 200;

    public static readonly IReadOnlyList<string> All =
        [SetStatus, SetDueDate, ClearDueDate, Assign, UnassignAll, Archive, SetPriority, Delete];
}

/// <summary>
/// Applies one change (status, due date, assignee, priority, archive or delete) to several items of one board as one unit:
/// every item changes or none does (the data session owns the transaction and an item that cannot change
/// throws before anything commits). The request is authorized once on the board, and board items take their
/// access from their board, so that is the per-item check; every named item must be a live item of that
/// board. The per-item writes are the ones their own commands make, so a change here is the change those
/// commands would make one by one. Each item carries the version the caller last read: one stale item refuses
/// the whole request, names the first such item and changes nothing.
/// </summary>
/// <param name="ExpectedVersions">The version the caller last read of every named item, by item id.</param>
/// <param name="Operation">One of <see cref="BulkBoardItemOperations.All"/>.</param>
/// <param name="Status">The status option id for <c>set-status</c>.</param>
/// <param name="DueDate">The due date (UTC) for <c>set-due-date</c>.</param>
/// <param name="AssigneeUserId">The member who becomes the only assignee for <c>assign</c>.</param>
/// <param name="Priority">The option id of the board's Priority field for <c>set-priority</c>.</param>
[IdempotencyOperation("work-management.board-items.bulk-update-board-items.v1")]
public record BulkUpdateBoardItemsCommand(
    Guid BoardId,
    string Operation,
    IReadOnlyList<Guid> ItemIds,
    IReadOnlyDictionary<Guid, long> ExpectedVersions,
    string? Status = null,
    DateTime? DueDate = null,
    Guid? AssigneeUserId = null,
    string? Priority = null) : ICommand<BulkUpdateBoardItemsResult>, IWriteRequest, IRequirePermission, IRequireAdditionalPermission, IAuthenticatedRequest, IResourceScopedRequest, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.UpdateItem;

    // Assigning people is its own permission, as it is item by item: a request that changes the
    // assignees needs it as well as UpdateItem, so the bulk change is never wider than the item ones.
    public IReadOnlyList<string> AdditionalActionNames =>
        Operation is BulkBoardItemOperations.Assign or BulkBoardItemOperations.UnassignAll
            ? [nameof(PermissionAction.AssignItem)]
            : [];

    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board"), BoardId);
}

public sealed record BulkUpdateBoardItemsResult(int Updated);

public class BulkUpdateBoardItemsCommandHandler : IRequestHandler<BulkUpdateBoardItemsCommand, BulkUpdateBoardItemsResult>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _timeProvider;
    private readonly IRequestHandler<UpdateBoardItemStatusCommand, Result> _status;
    private readonly IRequestHandler<ArchiveBoardItemCommand, Result> _archive;
    private readonly IRequestHandler<AssignBoardItemMemberCommand, Result> _assign;
    private readonly IRequestHandler<UpdateBoardItemFieldValueCommand, BoardItemSlimDto> _fieldValue;
    private readonly IRequestHandler<DeleteBoardItemCommand, Result> _delete;
    private readonly IBoardRealtimeNotifier? _realtime;

    public BulkUpdateBoardItemsCommandHandler(
        IWorkManagementDbContext context,
        ICurrentUser currentUser,
        IDateTimeProvider timeProvider,
        IRequestHandler<UpdateBoardItemStatusCommand, Result> status,
        IRequestHandler<ArchiveBoardItemCommand, Result> archive,
        IRequestHandler<AssignBoardItemMemberCommand, Result> assign,
        IRequestHandler<UpdateBoardItemFieldValueCommand, BoardItemSlimDto> fieldValue,
        IRequestHandler<DeleteBoardItemCommand, Result> delete,
        IBoardRealtimeNotifier? realtime = null)
    {
        _realtime = realtime;
        _context = context;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
        _status = status;
        _archive = archive;
        _assign = assign;
        _fieldValue = fieldValue;
        _delete = delete;
    }

    public async Task<BulkUpdateBoardItemsResult> Handle(BulkUpdateBoardItemsCommand request, CancellationToken ct)
    {
        var response = await HandleCore(request, ct);
        if (_realtime is not null) await _realtime.NotifyAsync(request, response, ct);
        return response;
    }

    private async Task<BulkUpdateBoardItemsResult> HandleCore(BulkUpdateBoardItemsCommand request, CancellationToken ct)
    {
        var ids = request.ItemIds.Distinct().ToList();

        var items = await _context.BoardItems
            .Where(i => ids.Contains(i.Id) && i.BoardId == request.BoardId && i.DeletedAt == null)
            .ToListAsync(ct);
        // An item of another board, or one that is gone, is not found: nothing is revealed about it.
        var missing = ids.FirstOrDefault(id => items.All(i => i.Id != id));
        if (missing != Guid.Empty) throw new NotFoundException(nameof(BoardItem), missing);

        // A stale item refuses the whole batch before anything is written; the first one is named.
        foreach (var id in ids)
        {
            var current = items.Single(i => i.Id == id).Version;
            if (request.ExpectedVersions.TryGetValue(id, out var seen) && seen == current) continue;
            throw new PreconditionFailedException(
                "An item was changed by someone else. Reload and retry.", "common.precondition-failed", request.ExpectedVersions.GetValueOrDefault(id), current)
            {
                Extensions = new Dictionary<string, object?> { ["itemId"] = id },
            };
        }

        var priorityField = request.Operation == BulkBoardItemOperations.SetPriority
            ? await ResolvePriorityFieldAsync(request.BoardId, ct)
            : null;

        var now = _timeProvider.UtcNow;
        foreach (var id in ids)
        {
            switch (request.Operation)
            {
                case BulkBoardItemOperations.SetStatus:
                    Require(await _status.Handle(new UpdateBoardItemStatusCommand(id, request.Status!, 0), ct));
                    break;

                case BulkBoardItemOperations.Archive:
                    Require(await _archive.Handle(new ArchiveBoardItemCommand(id, 0), ct));
                    break;

                case BulkBoardItemOperations.SetPriority:
                    // The same write the item's own priority change makes; no expected version, as for the rest.
                    await _fieldValue.Handle(new UpdateBoardItemFieldValueCommand(id, priorityField!.Id, request.Priority, 0), ct);
                    break;

                case BulkBoardItemOperations.Delete:
                    Require(await _delete.Handle(new DeleteBoardItemCommand(id, 0), ct));
                    break;

                case BulkBoardItemOperations.SetDueDate:
                case BulkBoardItemOperations.ClearDueDate:
                    var item = items.Single(i => i.Id == id);
                    var due = request.Operation == BulkBoardItemOperations.ClearDueDate
                        ? (DateTimeOffset?)null
                        : new DateTimeOffset(request.DueDate!.Value, TimeSpan.Zero);
                    item.SetTimeline(item.StartedAt, due, _currentUser.UserId, now);
                    break;

                case BulkBoardItemOperations.Assign:
                    await ReplaceAssigneesAsync(id, request.AssigneeUserId!.Value, ct);
                    break;

                case BulkBoardItemOperations.UnassignAll:
                    await ClearAssigneesAsync(id, keep: null, ct);
                    break;

                default:
                    throw new BusinessRuleException($"Unknown bulk operation '{request.Operation}'.");
            }
        }

        return new BulkUpdateBoardItemsResult(ids.Count);
    }

    // The board's Priority field, resolved the way every read resolves it: a board without one cannot take the change.
    private async Task<BoardField> ResolvePriorityFieldAsync(Guid boardId, CancellationToken ct)
    {
        var fields = await _context.BoardFields
            .Where(f => f.BoardId == boardId && f.DeletedAt == null)
            .ToListAsync(ct);
        return WorkItemSemanticFields.ResolvePriorityField(fields)
            ?? throw new BusinessRuleException("This board has no Priority field.");
    }

    // The member becomes the only assignee: the others are taken off, then the member is added (no-op when
    // already on the item), the same two steps the item's own assignee writes make.
    private async Task ReplaceAssigneesAsync(Guid itemId, Guid userId, CancellationToken ct)
    {
        await ClearAssigneesAsync(itemId, keep: userId, ct);
        Require(await _assign.Handle(new AssignBoardItemMemberCommand(itemId, userId, 0), ct));
    }

    private async Task ClearAssigneesAsync(Guid itemId, Guid? keep, CancellationToken ct)
    {
        var others = await _context.BoardItemMembers
            .Where(m => m.ItemId == itemId && (keep == null || m.UserId != keep))
            .ToListAsync(ct);
        _context.BoardItemMembers.RemoveRange(others);
        if (others.Count > 0)
        {
            var item = await _context.BoardItems.FirstAsync(i => i.Id == itemId, ct);
            item.NoteAssigneesChanged(_currentUser.UserId, _timeProvider.UtcNow);
        }
    }

    // A refusal rolls the whole request back: the session commits on a returned result, so it has to throw.
    private static void Require(Result result)
    {
        if (!result.Succeeded)
            throw new BusinessRuleException(result.Errors.FirstOrDefault() ?? "The change was refused.");
    }
}
