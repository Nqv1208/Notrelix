using BoardEntity = global::Notrelix.Domain.WorkManagement.Boards.Board;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.WorkManagement.Commands.Boards.RemoveBoardMember;
using global::Notrelix.Application.Features.WorkManagement.Commands.Boards;
using global::Notrelix.Application.Features.WorkManagement.DTOs;
using global::Notrelix.Domain.Identity;
using global::Notrelix.Domain.Workspaces;

namespace Notrelix.Application.Features.WorkManagement.Commands.Boards.RemoveBoardMember;

public record RemoveBoardMemberCommand(Guid BoardId, Guid UserId) : IRequest<Result>;

public class RemoveBoardMemberCommandHandler : IRequestHandler<RemoveBoardMemberCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IWorkspacePermissionService _permissions;

    public RemoveBoardMemberCommandHandler(
        IApplicationDbContext context,
        ICurrentUser currentUser,
        IWorkspacePermissionService permissions)
    {
        _context = context;
        _currentUser = currentUser;
        _permissions = permissions;
    }

    public async Task<Result> Handle(RemoveBoardMemberCommand request, CancellationToken ct)
    {
        var board = await _context.Boards
            .Include(b => b.Members)
            .FirstOrDefaultAsync(b => b.Id == request.BoardId, ct);

        if (board is null) throw new NotFoundException(nameof(BoardEntity), request.BoardId);

        await _permissions.EnsureCanManageBoardAsync(board.Id, _currentUser.UserId, ct);

        board.RemoveMember(request.UserId);
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
