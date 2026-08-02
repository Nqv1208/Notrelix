using BoardEntity = global::Notrelix.Domain.WorkManagement.Boards.Board;
using BoardMemberEntity = global::Notrelix.Domain.WorkManagement.Boards.BoardMember;
using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Boards.Commands.AddBoardMember;

[IdempotencyOperation("work-management.boards.add-board-member.v1")]
public record AddBoardMemberCommand(Guid BoardId, Guid UserId, BoardRole? Role, string? IdempotencyKey = null) : ICommand<Result>, ITransactionalRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.ManageBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.Board, BoardId);
    string IIdempotentRequest.IdempotencyKey => IdempotencyKey ?? $"add-board-member:{BoardId}:{UserId}";
}

public class AddBoardMemberCommandHandler : IRequestHandler<AddBoardMemberCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IWorkspaceAccessResolver _workspaceAccess;

    public AddBoardMemberCommandHandler(
        IWorkManagementDbContext context,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider,
        IWorkspaceAccessResolver workspaceAccess)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
        _workspaceAccess = workspaceAccess;
    }

    public async Task<Result> Handle(AddBoardMemberCommand request, CancellationToken ct)
    {
        var board = await _context.Boards
            .FirstOrDefaultAsync(b => b.Id == request.BoardId, ct);

        if (board is null) throw new NotFoundException(nameof(BoardEntity), request.BoardId);

        var access = await _workspaceAccess.ResolveAsync(board.WorkspaceId, request.UserId, ct);
        if (!access.CanAccess)
        {
            throw new Notrelix.Domain.Common.Exceptions.BusinessRuleException(
                "BoardMemberMustBelongToWorkspace",
                "Board member must belong to the same workspace.");
        }

        var alreadyMember = await _context.BoardMembers
            .AnyAsync(m => m.BoardId == board.Id && m.UserId == request.UserId, ct);
        if (alreadyMember) return Result.Success();

        var role = request.Role ?? BoardRole.Member;

        var member = BoardMemberEntity.Create(board.Id, request.UserId, role, _dateTimeProvider.UtcNow);
        _context.BoardMembers.Add(member);

        return Result.Success();
    }
}
