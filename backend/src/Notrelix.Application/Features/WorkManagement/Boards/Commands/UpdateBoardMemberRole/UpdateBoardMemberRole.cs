using BoardEntity = global::Notrelix.Domain.WorkManagement.Boards.Board;
using BoardMemberEntity = global::Notrelix.Domain.WorkManagement.Boards.BoardMember;
using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Governance.Public.Access;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Boards.Commands.UpdateBoardMemberRole;

[IdempotencyOperation("work-management.boards.update-board-member-role.v1")]
public record UpdateBoardMemberRoleCommand(Guid BoardId, Guid UserId, BoardRole Role)
    : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequireGrantPermission, IRequirePermissionTarget, IIdempotentRequest
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

    public UpdateBoardMemberRoleCommandHandler(
        IWorkManagementDbContext context,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(UpdateBoardMemberRoleCommand request, CancellationToken ct)
    {
        var board = await _context.Boards
            .FirstOrDefaultAsync(b => b.Id == request.BoardId, ct);
        if (board is null) throw new NotFoundException(nameof(BoardEntity), request.BoardId);

        var member = await _context.BoardMembers
            .FirstOrDefaultAsync(m => m.BoardId == request.BoardId && m.UserId == request.UserId, ct);
        if (member is null) throw new NotFoundException(nameof(BoardMemberEntity), request.UserId);

        member.UpdateRole(request.Role);
        return Result.Success();
    }
}
