using BoardEntity = global::Notrelix.Domain.WorkManagement.Boards.Board;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.WorkManagement.Common.DTOs;
using global::Notrelix.Domain.Identity;
using global::Notrelix.Domain.Workspaces;

namespace Notrelix.Application.Features.WorkManagement.Boards.Commands.UpdateBoard;

public record UpdateBoardCommand(Guid BoardId, string? Title, string? Description, string? Background, string? Visibility) : ICommand<Result>, ITransactionalRequest;

public class UpdateBoardCommandHandler : IRequestHandler<UpdateBoardCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IWorkspacePermissionService _permissions;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateBoardCommandHandler(
        IApplicationDbContext context,
        ICurrentUser currentUser,
        IWorkspacePermissionService permissions,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _permissions = permissions;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(UpdateBoardCommand request, CancellationToken ct)
    {
        var board = await _context.Boards.FirstOrDefaultAsync(b => b.Id == request.BoardId, ct);
        if (board is null) throw new NotFoundException(nameof(BoardEntity), request.BoardId);

        await _permissions.EnsureCanManageBoardAsync(board.Id, _currentUser.UserId, ct);

        var now = _dateTimeProvider.UtcNow;
        if (request.Title is not null) board.Rename(request.Title, _currentUser.UserId, now);
        if (request.Description is not null) board.UpdateDescription(request.Description, _currentUser.UserId, now);
        if (request.Background is not null) board.UpdateBackground(request.Background, _currentUser.UserId, now);
        if (request.Visibility is not null)
        {
            var visibility = Enum.Parse<BoardVisibility>(request.Visibility, ignoreCase: true);
            board.ChangeVisibility(visibility, _currentUser.UserId, now);
        }

        return Result.Success();
    }
}
