using Notrelix.Application.Features.WorkManagement.Common.DTOs;
using Notrelix.Application.Features.WorkManagement.BoardItems.Services;
using Notrelix.Application.Features.WorkManagement.Ordering;
namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.MoveBoardItem;

[IdempotencyOperation("work-management.board-items.move-board-item.v1")]
public record MoveBoardItemCommand(
    Guid ItemId,
    Guid NewGroupId,
    long ExpectedVersion,
    Guid? PreviousSiblingId = null,
    Guid? NextSiblingId = null) : ICommand<BoardItemSlimDto>, IWriteRequest, IRequirePermission, IAuthenticatedRequest, IResourceScopedRequest, IIdempotentRequest, IExpectedVersionRequest
{
    public PermissionAction Action => PermissionAction.MoveItem;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board-item"), ItemId);
}

public class MoveBoardItemCommandHandler : IRequestHandler<MoveBoardItemCommand, BoardItemSlimDto>
{
    private readonly MoveBoardItemUseCase _useCase;
    private readonly ICurrentUser _currentUser;
    private readonly IBoardRealtimeNotifier? _realtime;

    public MoveBoardItemCommandHandler(
        MoveBoardItemUseCase useCase,
        ICurrentUser currentUser,
        IBoardRealtimeNotifier? realtime = null)
    {
        _realtime = realtime;
        _useCase = useCase;
        _currentUser = currentUser;
    }

    public async Task<BoardItemSlimDto> Handle(MoveBoardItemCommand request, CancellationToken cancellationToken)
    {
        var response = await HandleCore(request, cancellationToken);
        if (_realtime is not null) await _realtime.NotifyAsync(request, response, cancellationToken);
        return response;
    }

    private async Task<BoardItemSlimDto> HandleCore(MoveBoardItemCommand request, CancellationToken cancellationToken)
    {
        var outcome = await _useCase.MoveAsync(
            request.ItemId,
            request.NewGroupId,
            new WorkPlacementIntent(request.PreviousSiblingId, request.NextSiblingId),
            _currentUser.UserId,
            cancellationToken);
        return outcome.Item;
    }
}
