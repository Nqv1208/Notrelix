using BoardEntity = global::Notrelix.Domain.WorkManagement.Boards.Board;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Boards.Services;

namespace Notrelix.Application.Features.WorkManagement.Boards.Commands.UpdateBoardVisibility;

[IdempotencyOperation("work-management.boards.update-board-visibility.v1")]
public record UpdateBoardVisibilityCommand(
    Guid BoardId,
    BoardVisibility Visibility,
    long ExpectedVersion)
    : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IExpectedVersionRequest, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.ManageBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board"), BoardId);
}

public class UpdateBoardVisibilityCommandHandler : IRequestHandler<UpdateBoardVisibilityCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly BoardAccessChangeLog _changeLog;
    private readonly IBoardRealtimeNotifier? _realtime;

    public UpdateBoardVisibilityCommandHandler(
        IWorkManagementDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider,
        BoardAccessChangeLog changeLog,
        IBoardRealtimeNotifier? realtime = null)
    {
        _realtime = realtime;
        _changeLog = changeLog;
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(UpdateBoardVisibilityCommand request, CancellationToken ct)
    {
        var response = await HandleCore(request, ct);
        if (_realtime is not null) await _realtime.NotifyAsync(request, response, ct);
        return response;
    }

    private async Task<Result> HandleCore(UpdateBoardVisibilityCommand request, CancellationToken ct)
    {
        var board = await _context.Boards.FirstOrDefaultAsync(b => b.Id == request.BoardId, ct);
        if (board is null) throw new NotFoundException(nameof(BoardEntity), request.BoardId);

        var previous = board.Visibility;
        board.ChangeVisibility(request.Visibility, _requestContext.UserId, _dateTimeProvider.UtcNow);
        if (previous != board.Visibility)
        {
            // Going Private ends every Space and workspace source: the person who does it stays on the board.
            if (board.Visibility == BoardVisibility.Private
                && await BoardOwnership.EnsureActorStaysAsync(_context, board.Id, _requestContext.UserId, _dateTimeProvider.UtcNow, ct))
                await _changeLog.MemberAddedAsync(board.Id, _requestContext.UserId, BoardRole.Owner, ct);
            await _changeLog.VisibilityChangedAsync(board.Id, board.Visibility, ct);
        }
        return Result.Success();
    }
}
