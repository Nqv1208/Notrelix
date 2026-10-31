using BoardEntity = global::Notrelix.Domain.WorkManagement.Boards.Board;
using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Governance.Public.Access;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Boards.Services;

namespace Notrelix.Application.Features.WorkManagement.Boards.Commands.LeaveBoard;

/// <summary>
/// A person ends their own explicit board membership (any Board role). Grants they hold are Governance's and are
/// not touched; on a Workspace-visible Board they still see it through the Workspace. The last Owner and the last
/// Manager cannot leave. Leaving twice changes nothing.
/// </summary>
[IdempotencyOperation("work-management.boards.leave-board.v1")]
public record LeaveBoardCommand(Guid BoardId) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    // Whoever can read the Board may leave it; a person without access gets the same NotFound as a missing Board.
    public PermissionAction Action => PermissionAction.ViewBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board"), BoardId);
}

public class LeaveBoardCommandHandler : IRequestHandler<LeaveBoardCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly BoardAccessChangeLog _changeLog;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IBoardSharingSummary _sharing;
    private readonly IBoardRealtimeNotifier? _realtime;

    public LeaveBoardCommandHandler(
        IWorkManagementDbContext context,
        ICurrentUser currentUser,
        BoardAccessChangeLog changeLog,
        IDateTimeProvider dateTimeProvider,
        IBoardSharingSummary sharing,
        IBoardRealtimeNotifier? realtime = null)
    {
        _realtime = realtime;
        _dateTimeProvider = dateTimeProvider;
        _sharing = sharing;
        _changeLog = changeLog;
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(LeaveBoardCommand request, CancellationToken ct)
    {
        var response = await HandleCore(request, ct);
        if (_realtime is not null) await _realtime.NotifyAsync(request, response, ct);
        return response;
    }

    private async Task<Result> HandleCore(LeaveBoardCommand request, CancellationToken ct)
    {
        var board = await _context.Boards.FirstOrDefaultAsync(b => b.Id == request.BoardId, ct);
        if (board is null) throw new NotFoundException(nameof(BoardEntity), request.BoardId);

        var userId = _currentUser.UserId;
        var member = await _context.BoardMembers.FirstOrDefaultAsync(m => m.BoardId == board.Id && m.UserId == userId, ct);
        // Not an explicit member (already left, or reaching the Board another way): nothing to end.
        if (member is null) return Result.Success();

        if (member.Role == BoardRole.Owner)
        {
            var anotherOwner = await _context.BoardMembers.AnyAsync(
                m => m.BoardId == board.Id && m.UserId != userId && m.Role == BoardRole.Owner, ct);
            if (!anotherOwner)
            {
                throw new Notrelix.Domain.Common.Exceptions.BusinessRuleException(
                    WorkManagementRuleCodes.WorkManagement_Board_LastOwner,
                    "The last owner can't leave the board. Make someone else an owner first.");
            }
        }

        if (BoardRoleLevels.RankOf(member.Role.ToString()) >= 4)
            await BoardManagerGuard.EnsureAnotherManagerAsync(_sharing, board.Id, userId, ct);

        _context.BoardMembers.Remove(member);
        board.RecordAccessChange(userId, _dateTimeProvider.UtcNow);
        await _changeLog.MemberRemovedAsync(board.Id, userId, member.Role, ct);
        return Result.Success();
    }
}
