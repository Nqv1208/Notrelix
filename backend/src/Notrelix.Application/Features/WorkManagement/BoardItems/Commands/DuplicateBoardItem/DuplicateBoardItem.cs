using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.BoardGroups.Commands.DuplicateBoardGroup;
using Notrelix.Application.Features.WorkManagement.BoardItems.Services;

using Notrelix.Application.Features.WorkManagement.Ordering;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.DuplicateBoardItem;

[IdempotencyOperation("work-management.board-items.duplicate-board-item.v1")]
public record DuplicateBoardItemCommand(Guid BoardItemId) : ICommand<Result<Guid>>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
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
        var source = await _context.BoardItems
            .FirstOrDefaultAsync(c => c.Id == request.BoardItemId && c.DeletedAt == null, ct);
        if (source is null) throw new NotFoundException(nameof(BoardItem), request.BoardItemId);

        // The copy goes directly after its source, under the group's ordering lock (WMREQ049).
        var nextPosition = await _placement.ResolveAfterAsync(
            new WorkOrderingScope(WorkOrderingScopeKind.GroupItems, source.GroupId),
            source.Id,
            token => GroupItemSiblings.LoadAsync(_context, source.GroupId, token),
            ct);

        var now = _timeProvider.UtcNow;
        // The copy is a new Item with its own key, allocated after the ordering lock.
        var key = (await _keys.AllocateAsync(_context, source.BoardId, 1, ct))[0];

        var duplicate = DuplicateBoardGroupCommandHandler.CloneCard(
            source,
            _requestContext.RequireAccountId(),
            source.GroupId,
            source.BoardId,
            source.WorkspaceId,
            _requestContext.UserId,
            $"{source.Name} copy",
            nextPosition,
            now,
            key);

        _context.BoardItems.Add(duplicate);
        return Result<Guid>.Success(duplicate.Id);
    }
}
