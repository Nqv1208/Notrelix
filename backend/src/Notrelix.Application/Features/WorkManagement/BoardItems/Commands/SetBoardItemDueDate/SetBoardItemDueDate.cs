using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.SetBoardItemDueDate;

[IdempotencyOperation("work-management.board-items.set-board-item-due-date.v1")]
public record SetBoardItemDueDateCommand(Guid BoardItemId, DateTime? DueDate, DateTime? StartDate) : ICommand<Result>, IWriteRequest, IIdempotentRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.UpdateItem;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board-item"), BoardItemId);
}

public class SetBoardItemDueDateCommandHandler : IRequestHandler<SetBoardItemDueDateCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _timeProvider;
    private readonly IBoardRealtimeNotifier? _realtime;

    public SetBoardItemDueDateCommandHandler(
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

    public async Task<Result> Handle(SetBoardItemDueDateCommand request, CancellationToken ct)
    {
        var response = await HandleCore(request, ct);
        if (_realtime is not null) await _realtime.NotifyAsync(request, response, ct);
        return response;
    }

    private async Task<Result> HandleCore(SetBoardItemDueDateCommand request, CancellationToken ct)
    {
        var card = await _context.BoardItems
            .FirstOrDefaultAsync(c => c.Id == request.BoardItemId, ct);
        if (card is null) throw new NotFoundException(nameof(BoardItem), request.BoardItemId);

        var now = _timeProvider.UtcNow;
        card.SetTimeline(
            request.StartDate.HasValue ? new DateTimeOffset(request.StartDate.Value, TimeSpan.Zero) : card.StartedAt,
            request.DueDate.HasValue ? new DateTimeOffset(request.DueDate.Value, TimeSpan.Zero) : card.DueAt,
            _currentUser.UserId,
            now);

        return Result.Success();
    }
}
