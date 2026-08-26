using Notrelix.Application.Features.WorkManagement.Common.DTOs;
using Notrelix.Application.Features.WorkManagement.Abstractions;

using Notrelix.Domain.SharedKernel.Ordering;
namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.MoveBoardItem;

[IdempotencyOperation("work-management.board-items.move-board-item.v1")]
public record MoveBoardItemCommand(
    Guid ItemId,
    Guid NewGroupId,
    double Position) : ICommand<BoardItemSlimDto>, IWriteRequest, IRequirePermission, IAuthenticatedRequest, IResourceScopedRequest, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.MoveItem;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board-item"), ItemId);
}

public class MoveBoardItemCommandHandler : IRequestHandler<MoveBoardItemCommand, BoardItemSlimDto>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _timeProvider;
    private readonly IRealtimeChangeMapper<MoveBoardItemCommand, BoardItemSlimDto>? _realtime;
    private readonly IIntegrationEventCollector? _events;

    public MoveBoardItemCommandHandler(IWorkManagementDbContext context, ICurrentUser currentUser, IDateTimeProvider timeProvider, IRealtimeChangeMapper<MoveBoardItemCommand, BoardItemSlimDto>? realtime = null, IIntegrationEventCollector? events = null)
    {
        _context = context;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
        _realtime = realtime;
        _events = events;
    }

    public async Task<BoardItemSlimDto> Handle(MoveBoardItemCommand request, CancellationToken cancellationToken)
    {
        var item = await _context.BoardItems
            .FirstOrDefaultAsync(item => item.Id == request.ItemId, cancellationToken);

        if (item == null)
            throw new NotFoundException("BoardItem", request.ItemId);

        var group = await _context.BoardGroups
            .FirstOrDefaultAsync(g => g.Id == request.NewGroupId && g.BoardId == item.BoardId, cancellationToken);

        if (group == null)
            throw new NotFoundException("BoardGroup", request.NewGroupId);

        var now = _timeProvider.UtcNow;
        var position = FractionalIndexGenerator.GenerateKeyBetween(null, null);

        item.MoveToGroup(BoardGroupRef.From(group), position, _currentUser.UserId, now);

        var memberIds = await _context.BoardItemMembers
            .Where(m => m.ItemId == item.Id)
            .Select(m => m.UserId)
            .ToListAsync(cancellationToken);

        var labelIds = await _context.BoardItemLabels
            .Where(l => l.ItemId == item.Id)
            .Select(l => l.LabelId)
            .ToListAsync(cancellationToken);

        var response = new BoardItemSlimDto(
            item.Id,
            item.GroupId,
            item.Name,
            item.Position.Value,
            memberIds,
            labelIds
        );
        if (_realtime is not null && _events is not null)
            _events.Add(_realtime.Map(request, response, item.Version));
        return response;
    }
}

public sealed class MoveBoardItemRealtimeMapper(IExecutionContextReader context, IDateTimeProvider time)
    : RealtimeChangeMapper<MoveBoardItemCommand, BoardItemSlimDto>(context, time)
{
    public override RealtimeResourceChangedV1 Map(MoveBoardItemCommand request, BoardItemSlimDto response, long streamVersion) =>
        Create("board", "Board", request.ItemId, "MoveBoardItem", response, streamVersion);
}
