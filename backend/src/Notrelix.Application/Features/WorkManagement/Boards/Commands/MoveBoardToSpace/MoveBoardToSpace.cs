using BoardEntity = global::Notrelix.Domain.WorkManagement.Boards.Board;
using BoardMemberEntity = global::Notrelix.Domain.WorkManagement.Boards.BoardMember;
using Notrelix.Application.Features.Governance.Public.Access;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Boards.Services;

namespace Notrelix.Application.Features.WorkManagement.Boards.Commands.MoveBoardToSpace;

/// <summary>
/// Moves a Board into a Workspaces Space, or back to the Workspace root
/// (<paramref name="SpaceId"/> null). Authorized on the source Board
/// (<c>ManageBoard</c>, pipeline) and on the target (the create-a-Board right
/// there, <see cref="IBoardSpacePlacement"/>; GOV-028). Guarded by the Board's
/// expected version; the same Space is a no-op that does not re-authorize.
/// <paramref name="AddUserIds"/> adds those people as Board members who can edit in the same transaction
/// (people already on the Board keep their role), so people who would lose the Board keep it; the move and
/// the adds stand or fall together.
/// </summary>
[IdempotencyOperation("work-management.boards.move-board-to-space.v1")]
public record MoveBoardToSpaceCommand(
    Guid BoardId,
    Guid? SpaceId,
    long ExpectedVersion,
    IReadOnlyList<Guid>? AddUserIds = null) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IExpectedVersionRequest, IIdempotentRequest, IRequireGrantPermission
{
    int IRequireGrantPermission.RequestedPermissionRank => BoardRoleLevels.RankOf(nameof(BoardRole.Member));
    public PermissionAction Action => PermissionAction.ManageBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board"), BoardId);
}

public class MoveBoardToSpaceCommandHandler : IRequestHandler<MoveBoardToSpaceCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly IBoardSpacePlacement _placement;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IBoardRealtimeNotifier? _realtime;
    private readonly IWorkspaceAccessResolver _workspaceAccess;
    private readonly BoardAccessChangeLog _changeLog;

    public MoveBoardToSpaceCommandHandler(
        IWorkManagementDbContext context,
        IBoardSpacePlacement placement,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider,
        IWorkspaceAccessResolver workspaceAccess,
        BoardAccessChangeLog changeLog,
        IBoardRealtimeNotifier? realtime = null)
    {
        _workspaceAccess = workspaceAccess;
        _changeLog = changeLog;
        _realtime = realtime;
        _context = context;
        _placement = placement;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(MoveBoardToSpaceCommand request, CancellationToken ct)
    {
        var response = await HandleCore(request, ct);
        if (_realtime is not null) await _realtime.NotifyAsync(request, response, ct);
        return response;
    }

    private async Task<Result> HandleCore(MoveBoardToSpaceCommand request, CancellationToken ct)
    {
        var board = await _context.Boards.FirstOrDefaultAsync(b => b.Id == request.BoardId, ct)
            ?? throw new NotFoundException(nameof(BoardEntity), request.BoardId);

        if (board.SpaceId == request.SpaceId)
            return Result.Success();

        await _placement.AuthorizeTargetAsync(board.WorkspaceId, request.SpaceId, ct);
        if (request.SpaceId is { } spaceId)
            await _placement.EnsureActiveTargetAsync(board.WorkspaceId, spaceId, ct);

        var already = (await _context.BoardMembers
            .Where(m => m.BoardId == board.Id)
            .Select(m => m.UserId)
            .ToListAsync(ct)).ToHashSet();
        foreach (var userId in (request.AddUserIds ?? []).Distinct().Where(id => !already.Contains(id)))
        {
            var access = await _workspaceAccess.ResolveAsync(board.WorkspaceId, userId, ct);
            if (!access.CanAccess)
                throw new Notrelix.Domain.Common.Exceptions.BusinessRuleException(
                    "BoardMemberMustBelongToWorkspace",
                    "Board member must belong to the same workspace.");

            _context.BoardMembers.Add(BoardMemberEntity.Create(board.Id, userId, BoardRole.Member, _dateTimeProvider.UtcNow));
            await _changeLog.MemberAddedAsync(board.Id, userId, BoardRole.Member, ct);
        }

        board.PlaceInSpace(request.SpaceId, _requestContext.UserId, _dateTimeProvider.UtcNow);
        return Result.Success();
    }
}
