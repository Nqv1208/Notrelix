using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.DeleteBoardItem;

[IdempotencyOperation("work-management.board-items.delete-board-item.v1")]
public record DeleteBoardItemCommand(Guid BoardItemId) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.UpdateItem;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board-item"), BoardItemId);
}

public class DeleteBoardItemCommandHandler : IRequestHandler<DeleteBoardItemCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _timeProvider;
    private readonly IBoardRealtimeNotifier? _realtime;

    public DeleteBoardItemCommandHandler(
        IWorkManagementDbContext context,
        ICurrentUser currentUser,
        IDateTimeProvider timeProvider,
        IBoardRealtimeNotifier? realtime = null)
    {
        _realtime = realtime;
        _context = context;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result> Handle(DeleteBoardItemCommand request, CancellationToken ct)
    {
        var response = await HandleCore(request, ct);
        if (_realtime is not null) await _realtime.NotifyAsync(request, response, ct);
        return response;
    }

    private async Task<Result> HandleCore(DeleteBoardItemCommand request, CancellationToken ct)
    {
        var item = await _context.BoardItems
            .FirstOrDefaultAsync(i => i.Id == request.BoardItemId, ct);
        if (item is null) throw new NotFoundException(nameof(BoardItem), request.BoardItemId);

        item.Delete(_currentUser.UserId, _timeProvider.UtcNow);
        return Result.Success();
    }
}
