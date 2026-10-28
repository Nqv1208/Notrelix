using BoardEntity = global::Notrelix.Domain.WorkManagement.Boards.Board;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Boards.Commands.UpdateBoard;

[IdempotencyOperation("work-management.boards.update-board.v1")]
public record UpdateBoardCommand(
    Guid BoardId,
    string? Title,
    string? Description,
    string? Background,
    BoardVisibility? Visibility,
    long ExpectedVersion,
    string? ItemKeyPrefix = null) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IExpectedVersionRequest, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.ManageBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board"), BoardId);
}

public class UpdateBoardCommandHandler : IRequestHandler<UpdateBoardCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IBoardRealtimeNotifier? _realtime;

    public UpdateBoardCommandHandler(
        IWorkManagementDbContext context,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider,
        IBoardRealtimeNotifier? realtime = null)
    {
        _realtime = realtime;
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(UpdateBoardCommand request, CancellationToken ct)
    {
        var response = await HandleCore(request, ct);
        if (_realtime is not null) await _realtime.NotifyAsync(request, response, ct);
        return response;
    }

    private async Task<Result> HandleCore(UpdateBoardCommand request, CancellationToken ct)
    {
        var board = await _context.Boards.FirstOrDefaultAsync(b => b.Id == request.BoardId, ct);
        if (board is null) throw new NotFoundException(nameof(BoardEntity), request.BoardId);

        var now = _dateTimeProvider.UtcNow;
        if (request.Title is not null) board.Rename(request.Title, _currentUser.UserId, now);
        if (request.Description is not null) board.UpdateDescription(request.Description, _currentUser.UserId, now);
        if (request.Background is not null) board.UpdateBackground(request.Background, _currentUser.UserId, now);
        if (request.ItemKeyPrefix is not null)
        {
            var prefix = request.ItemKeyPrefix.Trim().ToUpperInvariant();
            var taken = await _context.Boards.AnyAsync(
                b => b.Id != board.Id && b.WorkspaceId == board.WorkspaceId && b.DeletedAt == null
                     && b.ItemKeyPrefix != null && b.ItemKeyPrefix.ToUpper() == prefix, ct);
            board.ChangeItemKeyPrefix(prefix, taken, _currentUser.UserId, now);
        }
        if (request.Visibility is not null)
        {
            board.ChangeVisibility(request.Visibility.Value, _currentUser.UserId, now);
        }

        return Result.Success();
    }
}
