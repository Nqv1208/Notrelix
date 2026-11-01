using BoardEntity = global::Notrelix.Domain.WorkManagement.Boards.Board;
using BoardMemberEntity = global::Notrelix.Domain.WorkManagement.Boards.BoardMember;
using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Governance.Public.Access;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Boards.Services;

namespace Notrelix.Application.Features.WorkManagement.Boards.Commands.MakeBoardPrivate;

/// <summary>
/// Adds the chosen people to the Board as members who can edit, then makes the Board Private, in one
/// transaction (CTR-GAP-WM-BOARD-SHARING). People who are already members keep their role; the Board
/// stays as it was when any person cannot be added. Work Management owns both the members and the
/// visibility, so no cross-context transaction is involved. Needs Board management, and the added level
/// (Editor) is bounded by the caller's own level like a single member add.
/// </summary>
[IdempotencyOperation("work-management.boards.make-board-private.v1")]
public record MakeBoardPrivateCommand(Guid BoardId, IReadOnlyList<Guid> AddUserIds)
    : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IRequireGrantPermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.ManageBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board"), BoardId);

    int IRequireGrantPermission.RequestedPermissionRank => BoardRoleLevels.RankOf(nameof(BoardRole.Member));
}

public class MakeBoardPrivateCommandHandler : IRequestHandler<MakeBoardPrivateCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _clock;
    private readonly IWorkspaceAccessResolver _workspaceAccess;
    private readonly BoardAccessChangeLog _changeLog;
    private readonly IBoardRealtimeNotifier? _realtime;

    public MakeBoardPrivateCommandHandler(
        IWorkManagementDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider clock,
        IWorkspaceAccessResolver workspaceAccess,
        BoardAccessChangeLog changeLog,
        IBoardRealtimeNotifier? realtime = null)
    {
        _realtime = realtime;
        _context = context;
        _requestContext = requestContext;
        _clock = clock;
        _workspaceAccess = workspaceAccess;
        _changeLog = changeLog;
    }

    public async Task<Result> Handle(MakeBoardPrivateCommand request, CancellationToken ct)
    {
        var response = await HandleCore(request, ct);
        if (_realtime is not null) await _realtime.NotifyAsync(request, response, ct);
        return response;
    }

    private async Task<Result> HandleCore(MakeBoardPrivateCommand request, CancellationToken ct)
    {
        var board = await _context.Boards.FirstOrDefaultAsync(b => b.Id == request.BoardId, ct)
            ?? throw new NotFoundException(nameof(BoardEntity), request.BoardId);

        var existing = await _context.BoardMembers
            .Where(m => m.BoardId == board.Id)
            .Select(m => m.UserId)
            .ToListAsync(ct);
        var already = existing.ToHashSet();

        foreach (var userId in request.AddUserIds.Distinct().Where(id => !already.Contains(id)))
        {
            var access = await _workspaceAccess.ResolveAsync(board.WorkspaceId, userId, ct);
            if (!access.CanAccess)
            {
                throw new Notrelix.Domain.Common.Exceptions.BusinessRuleException(
                    "BoardMemberMustBelongToWorkspace",
                    "Board member must belong to the same workspace.");
            }

            _context.BoardMembers.Add(BoardMemberEntity.Create(board.Id, userId, BoardRole.Member, _clock.UtcNow));
            await _changeLog.MemberAddedAsync(board.Id, userId, BoardRole.Member, ct);
        }

        var previous = board.Visibility;
        board.ChangeVisibility(BoardVisibility.Private, _requestContext.UserId, _clock.UtcNow);
        if (previous != board.Visibility)
            await _changeLog.VisibilityChangedAsync(board.Id, board.Visibility, ct);

        return Result.Success();
    }
}

public class MakeBoardPrivateCommandValidator : AbstractValidator<MakeBoardPrivateCommand>
{
    public MakeBoardPrivateCommandValidator()
    {
        RuleFor(x => x.BoardId).NotEmpty();
        RuleFor(x => x.AddUserIds).NotNull();
        RuleFor(x => x.AddUserIds.Count).LessThanOrEqualTo(200).When(x => x.AddUserIds is not null);
        RuleForEach(x => x.AddUserIds).NotEmpty();
    }
}
