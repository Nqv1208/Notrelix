using BoardEntity = global::Notrelix.Domain.WorkManagement.Boards.Board;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.WorkManagement.Commands.Boards.UpdateBoard;
using global::Notrelix.Application.Features.WorkManagement.Commands.Boards;
using global::Notrelix.Application.Features.WorkManagement.DTOs;
using global::Notrelix.Domain.Identity;
using global::Notrelix.Domain.Workspaces;

namespace Notrelix.Application.Features.WorkManagement.Commands.Boards.UpdateBoard;

public record UpdateBoardCommand(Guid BoardId, string? Title, string? Description, string? Background, string? Visibility) : IRequest<Result>;

public class UpdateBoardCommandHandler : IRequestHandler<UpdateBoardCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IWorkspacePermissionService _permissions;

    public UpdateBoardCommandHandler(
        IApplicationDbContext context,
        ICurrentUser currentUser,
        IWorkspacePermissionService permissions)
    {
        _context = context;
        _currentUser = currentUser;
        _permissions = permissions;
    }

    public async Task<Result> Handle(UpdateBoardCommand request, CancellationToken ct)
    {
        var board = await _context.Boards.FirstOrDefaultAsync(b => b.Id == request.BoardId, ct);
        if (board is null) throw new NotFoundException(nameof(BoardEntity), request.BoardId);

        await _permissions.EnsureCanManageBoardAsync(board.Id, _currentUser.UserId, ct);

        if (request.Title is not null) board.Rename(request.Title, _currentUser.UserId);
        if (request.Description is not null) board.UpdateDescription(request.Description);
        if (request.Background is not null) board.UpdateBackground(request.Background);
        if (request.Visibility is not null)
        {
            var visibility = Enum.Parse<BoardVisibility>(request.Visibility, ignoreCase: true);
            board.UpdateVisibility(visibility);
        }

        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
