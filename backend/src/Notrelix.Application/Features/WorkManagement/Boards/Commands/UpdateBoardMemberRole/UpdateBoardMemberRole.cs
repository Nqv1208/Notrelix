using BoardEntity = global::Notrelix.Domain.WorkManagement.Boards.Board;
using BoardMemberEntity = global::Notrelix.Domain.WorkManagement.Boards.BoardMember;
using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Governance.Public.Access;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Boards.Services;

namespace Notrelix.Application.Features.WorkManagement.Boards.Commands.UpdateBoardMemberRole;

[IdempotencyOperation("work-management.boards.update-board-member-role.v1")]
public record UpdateBoardMemberRoleCommand(Guid BoardId, Guid UserId, BoardRole Role, long ExpectedVersion)
    : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequireGrantPermission, IRequirePermissionTarget, IExpectedVersionRequest, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.ManageBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board"), BoardId);

    // A board role carries a level: changing it is a grant of that level to
    // the target user, bounded by the caller's own level on the Board.
    int IRequireGrantPermission.RequestedPermissionRank => BoardRoleLevels.RankOf(Role.ToString());
    string? IRequirePermissionTarget.TargetSubjectType => "User";
    Guid? IRequirePermissionTarget.TargetSubjectId => UserId;
    Guid? IRequirePermissionTarget.TargetPermissionId => null;
}

public class UpdateBoardMemberRoleCommandHandler : IRequestHandler<UpdateBoardMemberRoleCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly BoardAccessChangeLog _changeLog;
    private readonly IBoardSharingSummary _sharing;
    private readonly IBoardRealtimeNotifier? _realtime;

    public UpdateBoardMemberRoleCommandHandler(
        IWorkManagementDbContext context,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider,
        BoardAccessChangeLog changeLog,
        IBoardSharingSummary sharing,
        IBoardRealtimeNotifier? realtime = null)
    {
        _sharing = sharing;
        _realtime = realtime;
        _changeLog = changeLog;
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(UpdateBoardMemberRoleCommand request, CancellationToken ct)
    {
        var response = await HandleCore(request, ct);
        if (_realtime is not null) await _realtime.NotifyAsync(request, response, ct);
        return response;
    }

    private async Task<Result> HandleCore(UpdateBoardMemberRoleCommand request, CancellationToken ct)
    {
        var board = await _context.Boards
            .FirstOrDefaultAsync(b => b.Id == request.BoardId, ct);
        if (board is null) throw new NotFoundException(nameof(BoardEntity), request.BoardId);

        var member = await _context.BoardMembers
            .FirstOrDefaultAsync(m => m.BoardId == request.BoardId && m.UserId == request.UserId, ct);
        if (member is null) throw new NotFoundException(nameof(BoardMemberEntity), request.UserId);

        var previous = member.Role;
        if (previous == request.Role) return Result.Success();

        // Lowering the only manager would leave the Board with nobody to manage it.
        if (BoardRoleLevels.RankOf(previous.ToString()) >= 4 && BoardRoleLevels.RankOf(request.Role.ToString()) < 4)
            await BoardManagerGuard.EnsureAnotherManagerAsync(_sharing, request.BoardId, request.UserId, ct);

        member.UpdateRole(request.Role);
        board.RecordAccessChange(_currentUser.UserId, _dateTimeProvider.UtcNow);
        await _changeLog.MemberRoleChangedAsync(request.BoardId, request.UserId, previous, request.Role, ct);
        return Result.Success();
    }
}
