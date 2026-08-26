using Notrelix.Application.Features.WorkManagement.Common.DTOs;
using Notrelix.Application.Features.WorkManagement.Abstractions;

using Notrelix.Domain.SharedKernel.Ordering;
namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.CreateBoardItem;

[IdempotencyOperation("work-management.board-items.create-board-item.v1")]
public record CreateBoardItemCommand(
    Guid BoardId,
    Guid GroupId,
    string Title,
    double Position) : ICommand<BoardItemSlimDto>, IWriteRequest, IRequirePermission, IAuthenticatedRequest, IResourceScopedRequest, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.CreateItem;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board"), BoardId);
}

public class CreateBoardItemCommandHandler : IRequestHandler<CreateBoardItemCommand, BoardItemSlimDto>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _timeProvider;
    private readonly IRealtimeChangeMapper<CreateBoardItemCommand, BoardItemSlimDto>? _realtime;
    private readonly IIntegrationEventCollector? _events;

    public CreateBoardItemCommandHandler(IWorkManagementDbContext context, ICurrentRequestContext requestContext, IDateTimeProvider timeProvider, IRealtimeChangeMapper<CreateBoardItemCommand, BoardItemSlimDto>? realtime = null, IIntegrationEventCollector? events = null)
    {
        _context = context;
        _requestContext = requestContext;
        _timeProvider = timeProvider;
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
        var position = FractionalIndexGenerator.GenerateKeyBetween(null, null);

        var item = BoardItem.CreateRoot(
            _requestContext.RequireAccountId(),
            _requestContext.RequireWorkspaceId(),
            request.BoardId,
            request.GroupId,
            request.Title,
            position,
            _requestContext.UserId,
            now);

        _context.BoardItems.Add(item);

        var response = new BoardItemSlimDto(
            item.Id,
            item.GroupId,
            item.Name,
            item.Position.Value,
            new List<Guid>(),
            new List<Guid>()
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
