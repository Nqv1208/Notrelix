using BoardEntity = global::Notrelix.Domain.Entities.Boards.Board;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using global::Notrelix.Application.Common.Interfaces;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Boards.Commands.Boards.SaveBoardView;
using global::Notrelix.Application.Features.Boards.Commands.Boards;
using global::Notrelix.Application.Features.Boards.DTOs;
using global::Notrelix.Domain.Common.Exceptions;
using global::Notrelix.Domain.Entities.Boards;
using global::Notrelix.Domain.Entities.Document;
using global::Notrelix.Domain.Entities.Identity;
using global::Notrelix.Domain.Entities.Shared;
using global::Notrelix.Domain.Entities.Workspaces;
using global::Notrelix.Domain.Enums;

namespace Notrelix.Application.Features.Boards.Commands.Boards.SaveBoardView;

public record SaveBoardViewCommand(Guid BoardId, string ViewMode, string? Filters) : IRequest<Result>;

public class SaveBoardViewCommandHandler : IRequestHandler<SaveBoardViewCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IWorkspacePermissionService _permissions;

    public SaveBoardViewCommandHandler(
        IApplicationDbContext context,
        ICurrentUser currentUser,
        IWorkspacePermissionService permissions)
    {
        _context = context;
        _currentUser = currentUser;
        _permissions = permissions;
    }

    public async Task<Result> Handle(SaveBoardViewCommand request, CancellationToken ct)
    {
        var workspaceId = await _context.Boards
            .AsNoTracking()
            .Where(board => board.Id == request.BoardId && !board.IsArchived)
            .Select(board => board.WorkspaceId)
            .FirstOrDefaultAsync(ct);
        if (workspaceId == Guid.Empty) throw new NotFoundException(nameof(BoardEntity), request.BoardId);

        if (!await _permissions.CanViewWorkspaceAsync(workspaceId, _currentUser.UserId, ct))
            throw new ForbiddenException("Bạn không có quyền xem workspace này.");

        var view = await _context.BoardViews
            .FirstOrDefaultAsync(v => v.BoardId == request.BoardId && v.UserId == _currentUser.UserId, ct);

        var viewMode = Enum.Parse<ViewMode>(request.ViewMode, ignoreCase: true);

        if (view is not null)
        {
            view.UpdateViewMode(viewMode);
            view.UpdateFilters(request.Filters ?? "{}");
        }
        else
        {
            view = BoardView.Create(request.BoardId, _currentUser.UserId, viewMode);
            view.UpdateFilters(request.Filters ?? "{}");
            _context.BoardViews.Add(view);
        }

        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
