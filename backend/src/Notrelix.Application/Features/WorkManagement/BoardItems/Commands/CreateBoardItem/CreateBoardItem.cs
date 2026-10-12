using Notrelix.Application.Features.WorkManagement.Common.DTOs;
using Notrelix.Application.Features.WorkManagement.Abstractions;

using Notrelix.Application.Features.WorkManagement.BoardItems.Services;
using Notrelix.Application.Features.WorkManagement.Ordering;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.CreateBoardItem;

[IdempotencyOperation("work-management.board-items.create-board-item.v1")]
public record CreateBoardItemCommand(
    Guid BoardId,
    Guid GroupId,
    string Title,
    Guid? PreviousSiblingId = null,
    Guid? NextSiblingId = null) : ICommand<BoardItemSlimDto>, IWriteRequest, IRequirePermission, IAuthenticatedRequest, IResourceScopedRequest, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.CreateItem;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board"), BoardId);
}

public class CreateBoardItemCommandHandler : IRequestHandler<CreateBoardItemCommand, BoardItemSlimDto>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _timeProvider;
    private readonly WorkPlacementResolver _placement;
    private readonly BoardItemKeyAllocator _keys;
    private readonly IRealtimeChangeMapper<CreateBoardItemCommand, BoardItemSlimDto>? _realtime;
    private readonly IIntegrationEventCollector? _events;

    public CreateBoardItemCommandHandler(IWorkManagementDbContext context, ICurrentRequestContext requestContext, IDateTimeProvider timeProvider, WorkPlacementResolver placement, BoardItemKeyAllocator keys, IRealtimeChangeMapper<CreateBoardItemCommand, BoardItemSlimDto>? realtime = null, IIntegrationEventCollector? events = null)
    {
        _context = context;
        _requestContext = requestContext;
        _timeProvider = timeProvider;
        _placement = placement;
        _keys = keys;
        _realtime = realtime;
        _events = events;
    }

    public async Task<BoardItemSlimDto> Handle(CreateBoardItemCommand request, CancellationToken cancellationToken)
    {
        var group = await _context.BoardGroups
            .FirstOrDefaultAsync(g => g.Id == request.GroupId && g.BoardId == request.BoardId, cancellationToken);

        if (group == null)
            throw new NotFoundException("BoardGroup", request.GroupId);

        var now = _timeProvider.UtcNow;
        var position = await _placement.ResolveAsync(
            new WorkOrderingScope(WorkOrderingScopeKind.GroupItems, group.Id),
            new WorkPlacementIntent(request.PreviousSiblingId, request.NextSiblingId),
            ct => GroupItemSiblings.LoadAsync(_context, group.Id, ct),
            cancellationToken);

        // The key is allocated last, after the ordering lock, so the board key
        // lock is always the innermost lock of the request.
        var key = (await _keys.AllocateAsync(request.BoardId, 1, _requestContext.UserId, now, cancellationToken))[0];

        var item = BoardItem.CreateRoot(
            _requestContext.RequireAccountId(),
            _requestContext.RequireWorkspaceId(),
            request.BoardId,
            request.GroupId,
            request.Title,
            position,
            _requestContext.UserId,
            now,
            itemKey: key.Key,
            itemSequence: key.Sequence);

        _context.BoardItems.Add(item);

        var response = new BoardItemSlimDto(
            item.Id,
            item.GroupId,
            item.Name,
            item.Position.Value,
            new List<Guid>(),
            new List<Guid>(),
            item.Version
        );
        if (_realtime is not null && _events is not null)
            _events.Add(_realtime.Map(request, response, item.Version));
        return response;
    }
}

public sealed class CreateBoardItemRealtimeMapper(IExecutionContextReader context, IDateTimeProvider time)
    : RealtimeChangeMapper<CreateBoardItemCommand, BoardItemSlimDto>(context, time)
{
    public override RealtimeResourceChangedV1 Map(CreateBoardItemCommand request, BoardItemSlimDto response, long streamVersion) =>
        Create("board", "Board", request.BoardId, "CreateBoardItem", response, streamVersion);
}
