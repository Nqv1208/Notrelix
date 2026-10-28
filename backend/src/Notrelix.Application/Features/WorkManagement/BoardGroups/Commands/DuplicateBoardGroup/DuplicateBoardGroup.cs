using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.BoardItems.Services;
using Notrelix.Application.Features.WorkManagement.Ordering;
using Notrelix.Domain.SharedKernel.Ordering;

namespace Notrelix.Application.Features.WorkManagement.BoardGroups.Commands.DuplicateBoardGroup;

[IdempotencyOperation("work-management.board-groups.duplicate-board-group.v1")]
public record DuplicateBoardGroupCommand(Guid GroupId) : ICommand<Result<Guid>>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
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

        var now = _timeProvider.UtcNow;

        var accountId = _requestContext.RequireAccountId();
        var duplicate = BoardGroup.Create(accountId, board.WorkspaceId, source.BoardId, $"{source.Title} copy", source.Color, nextPosition, _requestContext.UserId, now);
        _context.BoardGroups.Add(duplicate);

        var cards = await _context.BoardItems
            .AsNoTracking()
            .Where(c => c.GroupId == source.Id && c.DeletedAt == null)
            .OrderBy(c => c.Position)
            .ToListAsync(ct);

        // Every copy is a new Item with its own key, allocated after the ordering lock.
        var keys = await _keys.AllocateAsync(_context, board.Id, cards.Count, ct);
        for (var i = 0; i < cards.Count; i++)
        {
            var card = cards[i];
            _context.BoardItems.Add(CloneCard(card, accountId, duplicate.Id, board.Id, board.WorkspaceId, _requestContext.UserId, card.Name, card.Position, now, keys[i]));
        }

        return Result<Guid>.Success(duplicate.Id);
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
