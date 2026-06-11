using BoardEntity = global::Notrelix.Domain.WorkManagement.Boards.Board;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.WorkManagement.Commands.Boards.UnarchiveBoard;
using global::Notrelix.Application.Features.WorkManagement.Commands.Boards;
using global::Notrelix.Application.Features.WorkManagement.DTOs;
using global::Notrelix.Domain.Identity;
using global::Notrelix.Domain.Workspaces;

namespace Notrelix.Application.Features.WorkManagement.Commands.Boards.UnarchiveBoard;

public record UnarchiveBoardCommand(Guid BoardId) : IRequest<Result>;

public class UnarchiveBoardCommandHandler : IRequestHandler<UnarchiveBoardCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IWorkspacePermissionService _permissions;

    public UnarchiveBoardCommandHandler(
        IApplicationDbContext context,
        ICurrentUser currentUser,
        IWorkspacePermissionService permissions)
    {
        _context = context;
        _currentUser = currentUser;
        _permissions = permissions;
    }

    public async Task<Result> Handle(UnarchiveBoardCommand request, CancellationToken ct)
    {
        var board = await _context.Boards.FirstOrDefaultAsync(b => b.Id == request.BoardId, ct);
        if (board is null) throw new NotFoundException(nameof(BoardEntity), request.BoardId);
        await _permissions.EnsureCanManageBoardAsync(board.Id, _currentUser.UserId, ct);
        board.Unarchive();
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
