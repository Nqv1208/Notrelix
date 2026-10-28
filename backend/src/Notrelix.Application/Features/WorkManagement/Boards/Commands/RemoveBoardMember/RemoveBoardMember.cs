using BoardEntity = global::Notrelix.Domain.WorkManagement.Boards.Board;
using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Boards.Services;

namespace Notrelix.Application.Features.WorkManagement.Boards.Commands.RemoveBoardMember;

[IdempotencyOperation("work-management.boards.remove-board-member.v1")]
public record RemoveBoardMemberCommand(Guid BoardId, Guid UserId) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IRequirePermissionTarget, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.ManageBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board"), BoardId);

    // Removing a member acts on the target user's access: the caller's level
    // must reach the target's level on the Board.
    string? IRequirePermissionTarget.TargetSubjectType => "User";
    Guid? IRequirePermissionTarget.TargetSubjectId => UserId;
    Guid? IRequirePermissionTarget.TargetPermissionId => null;
}

public class RemoveBoardMemberCommandHandler : IRequestHandler<RemoveBoardMemberCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly BoardAccessChangeLog _changeLog;
    private readonly IBoardRealtimeNotifier? _realtime;

    public RemoveBoardMemberCommandHandler(
        IWorkManagementDbContext context,
        ICurrentUser currentUser,
        BoardAccessChangeLog changeLog,
        IBoardRealtimeNotifier? realtime = null)
    {
        _realtime = realtime;
        _changeLog = changeLog;
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(RemoveBoardMemberCommand request, CancellationToken ct)
    {
        var response = await HandleCore(request, ct);
        if (_realtime is not null) await _realtime.NotifyAsync(request, response, ct);
        return response;
    }

    private async Task<Result> HandleCore(RemoveBoardMemberCommand request, CancellationToken ct)
    {
        var board = await _context.Boards
            .FirstOrDefaultAsync(b => b.Id == request.BoardId, ct);

        if (board is null) throw new NotFoundException(nameof(BoardEntity), request.BoardId);

        var member = await _context.BoardMembers
            .FirstOrDefaultAsync(m => m.BoardId == board.Id && m.UserId == request.UserId, ct);

        if (member is not null)
        {
            _context.BoardMembers.Remove(member);
            await _changeLog.MemberRemovedAsync(board.Id, request.UserId, member.Role, ct);
        }

        return Result.Success();
    }
}
