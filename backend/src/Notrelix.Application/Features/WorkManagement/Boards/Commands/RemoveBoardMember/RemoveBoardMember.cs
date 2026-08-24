using BoardEntity = global::Notrelix.Domain.WorkManagement.Boards.Board;
using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Boards.Commands.RemoveBoardMember;

[IdempotencyOperation("work-management.boards.remove-board-member.v1")]
public record RemoveBoardMemberCommand(Guid BoardId, Guid UserId) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.ManageBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board"), BoardId);
}

public class RemoveBoardMemberCommandHandler : IRequestHandler<RemoveBoardMemberCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentUser _currentUser;

    public RemoveBoardMemberCommandHandler(
        IWorkManagementDbContext context,
        ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(RemoveBoardMemberCommand request, CancellationToken ct)
    {
        var board = await _context.Boards
            .FirstOrDefaultAsync(b => b.Id == request.BoardId, ct);

        if (board is null) throw new NotFoundException(nameof(BoardEntity), request.BoardId);

        var member = await _context.BoardMembers
            .FirstOrDefaultAsync(m => m.BoardId == board.Id && m.UserId == request.UserId, ct);

        if (member is not null)
            _context.BoardMembers.Remove(member);

        return Result.Success();
    }
}
