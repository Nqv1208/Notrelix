using BoardEntity = global::Notrelix.Domain.WorkManagement.Boards.Board;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.WorkManagement.Commands.Boards.AddBoardMember;
using global::Notrelix.Application.Features.WorkManagement.Commands.Boards;
using global::Notrelix.Application.Features.WorkManagement.DTOs;
using global::Notrelix.Domain.Identity;
using global::Notrelix.Domain.Workspaces;

namespace Notrelix.Application.Features.WorkManagement.Commands.Boards.AddBoardMember;

public record AddBoardMemberCommand(Guid BoardId, Guid UserId, string? Role) : IRequest<Result>;

public class AddBoardMemberCommandHandler : IRequestHandler<AddBoardMemberCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IWorkspacePermissionService _permissions;

    public AddBoardMemberCommandHandler(
        IApplicationDbContext context,
        ICurrentUser currentUser,
        IWorkspacePermissionService permissions)
    {
        _context = context;
        _currentUser = currentUser;
        _permissions = permissions;
    }

    public async Task<Result> Handle(AddBoardMemberCommand request, CancellationToken ct)
    {
        var board = await _context.Boards
            .Include(b => b.Members)
            .FirstOrDefaultAsync(b => b.Id == request.BoardId, ct);

        if (board is null) throw new NotFoundException(nameof(BoardEntity), request.BoardId);

        await _permissions.EnsureCanManageBoardAsync(board.Id, _currentUser.UserId, ct);

        var isWorkspaceMember = await _context.WorkspaceMembers
            .AsNoTracking()
            .AnyAsync(member => member.WorkspaceId == board.WorkspaceId && member.UserId == request.UserId, ct);
        if (!isWorkspaceMember)
        {
            throw new BusinessRuleViolationException(
                "BoardMemberMustBelongToWorkspace",
                "Board member must belong to the same workspace.");
        }

        var role = request.Role is not null
            ? Enum.Parse<BoardRole>(request.Role, ignoreCase: true)
            : BoardRole.Member;

        board.AddMember(request.UserId, role);
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
