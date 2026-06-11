using BoardEntity = global::Notrelix.Domain.WorkManagement.Boards.Board;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Common.Security;
using global::Notrelix.Application.Features.WorkManagement.Commands.Boards.SaveBoardView;
using global::Notrelix.Application.Features.WorkManagement.Commands.Boards;
using global::Notrelix.Application.Features.WorkManagement.DTOs;
using global::Notrelix.Domain.Identity;
using global::Notrelix.Domain.Workspaces;

namespace Notrelix.Application.Features.WorkManagement.Commands.Boards.SaveBoardView;

public record SaveBoardViewCommand(
    Guid WorkspaceId,
    Guid BoardId,
    string ViewMode,
    string? Filters) : IRequest<Result>, IAuthorizeableRequest
{
    ResourceType IAuthorizeableRequest.ResourceType => ResourceType.Board;
    Guid IAuthorizeableRequest.ResourceId => BoardId;
    PermissionAction IAuthorizeableRequest.Action => PermissionAction.ViewBoard;
}

public class SaveBoardViewCommandHandler : IRequestHandler<SaveBoardViewCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;

    public SaveBoardViewCommandHandler(
        IApplicationDbContext context,
        ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(SaveBoardViewCommand request, CancellationToken ct)
    {
        var boardExists = await _context.Boards
            .AsNoTracking()
            .AnyAsync(board => board.Id == request.BoardId && !board.IsArchived, ct);
        if (!boardExists) throw new NotFoundException(nameof(BoardEntity), request.BoardId);

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
            view = BoardView.Create(request.WorkspaceId, request.BoardId, _currentUser.UserId, viewMode);
            view.UpdateFilters(request.Filters ?? "{}");
            _context.BoardViews.Add(view);
        }

        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
