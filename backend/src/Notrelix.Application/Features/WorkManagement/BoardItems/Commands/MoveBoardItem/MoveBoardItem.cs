using Notrelix.Application.Features.WorkManagement.Common.DTOs;
using Notrelix.Application.Features.WorkManagement.BoardItems.Services;
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
    private readonly MoveBoardItemUseCase _useCase;
    private readonly ICurrentUser _currentUser;
    private readonly IRealtimeChangeMapper<MoveBoardItemCommand, BoardItemSlimDto>? _realtime;
    private readonly IIntegrationEventCollector? _events;

    public MoveBoardItemCommandHandler(
        MoveBoardItemUseCase useCase,
        ICurrentUser currentUser,
        IRealtimeChangeMapper<MoveBoardItemCommand, BoardItemSlimDto>? realtime = null,
        IIntegrationEventCollector? events = null)
    {
        _useCase = useCase;
        _currentUser = currentUser;
        _realtime = realtime;
        _events = events;
    }

    public async Task<BoardItemSlimDto> Handle(MoveBoardItemCommand request, CancellationToken cancellationToken)
    {
        var outcome = await _useCase.MoveAsync(
            request.ItemId,
            request.NewGroupId,
            _currentUser.UserId,
            cancellationToken);

        if (_realtime is not null && _events is not null)
            _events.Add(_realtime.Map(request, outcome.Item, outcome.Version));
        return outcome.Item;
    }
}

public sealed class MoveBoardItemRealtimeMapper(IExecutionContextReader context, IDateTimeProvider time)
    : RealtimeChangeMapper<MoveBoardItemCommand, BoardItemSlimDto>(context, time)
{
    public override RealtimeResourceChangedV1 Map(MoveBoardItemCommand request, BoardItemSlimDto response, long streamVersion) =>
        Create("board", "Board", request.ItemId, "MoveBoardItem", response, streamVersion);
}
