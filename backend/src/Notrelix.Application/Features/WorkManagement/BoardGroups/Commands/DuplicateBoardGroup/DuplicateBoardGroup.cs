using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.BoardItems.Services;
using Notrelix.Application.Features.WorkManagement.Ordering;
using Notrelix.Application.Features.WorkManagement.Templates.Services;
using Notrelix.Domain.SharedKernel.Ordering;

namespace Notrelix.Application.Features.WorkManagement.BoardGroups.Commands.DuplicateBoardGroup;

[IdempotencyOperation("work-management.board-groups.duplicate-board-group.v1")]
public record DuplicateBoardGroupCommand(Guid GroupId, string? Title = null) : ICommand<Result<Guid>>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    // Duplicating a group edits Board content (Editor), not Board management.
    public PermissionAction Action => PermissionAction.CreateItem;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board-group"), GroupId);
}

public class DuplicateBoardGroupCommandHandler : IRequestHandler<DuplicateBoardGroupCommand, Result<Guid>>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _timeProvider;
    private readonly WorkPlacementResolver _placement;
    private readonly IBoardItemKeyAllocator _keys;
    private readonly IBoardRealtimeNotifier? _realtime;

    public DuplicateBoardGroupCommandHandler(IWorkManagementDbContext context, ICurrentRequestContext requestContext, IDateTimeProvider timeProvider, WorkPlacementResolver placement, IBoardItemKeyAllocator keys, IBoardRealtimeNotifier? realtime = null)
    {
        _realtime = realtime;
        _keys = keys;
        _context = context;
        _requestContext = requestContext;
        _timeProvider = timeProvider;
        _placement = placement;
    }

    public async Task<Result<Guid>> Handle(DuplicateBoardGroupCommand request, CancellationToken ct)
    {
        var response = await HandleCore(request, ct);
        if (_realtime is not null) await _realtime.NotifyAsync(request, response, ct);
        return response;
    }

    private async Task<Result<Guid>> HandleCore(DuplicateBoardGroupCommand request, CancellationToken ct)
    {
        var source = await _context.BoardGroups
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == request.GroupId && l.DeletedAt == null, ct);
        if (source is null) throw new NotFoundException(nameof(BoardGroup), request.GroupId);

        // The copy goes directly after its source group, under the board's group
        // ordering lock; its cloned items keep their relative keys because they
        // enter a new, empty group (WMREQ048).
        var nextPosition = await _placement.ResolveAfterAsync(
            new WorkOrderingScope(WorkOrderingScopeKind.BoardGroups, source.BoardId),
            source.Id,
            token => BoardGroupSiblings.LoadAsync(_context, source.BoardId, token),
            ct);

        var board = await _context.Boards
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == source.BoardId, ct);
        if (board is null) throw new NotFoundException(nameof(Board), source.BoardId);

        // The copy takes the group's settings and its active top-level items, each with its active
        // sub-items; archived items stay behind. Past the item cap nothing is copied and nothing is cut.
        var items = await _context.BoardItems
            .AsNoTracking()
            .Where(c => c.GroupId == source.Id && c.DeletedAt == null && !c.IsArchived)
            .ToListAsync(ct);
        var roots = items
            .Where(c => c.ParentItemId is null)
            .OrderBy(c => c.Position.Value, StringComparer.Ordinal)
            .ToList();
        var rootIds = roots.Select(c => c.Id).ToHashSet();
        var subItems = items
            .Where(c => c.ParentItemId is { } parentId && rootIds.Contains(parentId))
            .OrderBy(c => c.Position.Value, StringComparer.Ordinal)
            .ToLookup(c => c.ParentItemId!.Value);
        var total = roots.Count + subItems.Sum(group => group.Count());
        if (total > BoardTemplateStructure.MaxItems)
            throw new Notrelix.Domain.Common.Exceptions.BusinessRuleException(
                WorkManagementRuleCodes.WorkManagement_Group_DuplicateTooManyItems,
                $"A group can be duplicated with at most {BoardTemplateStructure.MaxItems} items; this group has {total}. Archive or move some first.");

        var now = _timeProvider.UtcNow;
        var accountId = _requestContext.RequireAccountId();
        var duplicate = BoardGroup.Create(accountId, board.WorkspaceId, source.BoardId, CopyTitle(request.Title, source.Title), source.Color, nextPosition, _requestContext.UserId, now);
        _context.BoardGroups.Add(duplicate);

        // Every copy is a new Item with its own key, allocated after the ordering lock.
        var keys = await _keys.AllocateAsync(_context, board.Id, total, ct);
        var next = 0;
        foreach (var card in roots)
        {
            var copy = CloneCard(card, accountId, duplicate.Id, board.Id, board.WorkspaceId, _requestContext.UserId, card.Name, card.Position, now, keys[next++]);
            _context.BoardItems.Add(copy);
            foreach (var sub in subItems[card.Id])
            {
                var key = keys[next++];
                _context.BoardItems.Add(BoardItem.CreateSubItem(copy, sub.Name, sub.Position, _requestContext.UserId, now, key.Key, key.Sequence));
            }
        }

        return Result<Guid>.Success(duplicate.Id);
    }

    /// <summary>The client names the copy in its own language; only when it sends none does the server name it.</summary>
    private static string CopyTitle(string? requested, string sourceTitle)
    {
        if (!string.IsNullOrWhiteSpace(requested)) return requested;
        const string suffix = " copy";
        var room = 255 - suffix.Length;
        return (sourceTitle.Length > room ? sourceTitle[..room] : sourceTitle) + suffix;
    }

    internal static BoardItem CloneCard(BoardItem source, Guid accountId, Guid groupId, Guid boardId, Guid workspaceId, Guid createdByUserId, string name, FractionalIndex position, DateTimeOffset createdAt, BoardItemKey key)
    {
        var copy = BoardItem.CreateRoot(
            accountId,
            workspaceId,
            boardId,
            groupId,
            name,
            position,
            createdByUserId,
            createdAt,
            itemKey: key.Key,
            itemSequence: key.Sequence,
            startedAt: source.StartedAt,
            dueAt: source.DueAt,
            descriptionMd: source.DescriptionMd);

        return copy;
    }
}
