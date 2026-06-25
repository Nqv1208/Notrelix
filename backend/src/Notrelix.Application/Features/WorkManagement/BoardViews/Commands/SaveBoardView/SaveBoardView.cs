using BoardEntity = global::Notrelix.Domain.WorkManagement.Boards.Board;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.WorkManagement.Common.DTOs;
using global::Notrelix.Domain.SharedKernel;
using global::Notrelix.Domain.Identity;
using global::Notrelix.Domain.WorkManagement.Views;
using global::Notrelix.Domain.Workspaces;

namespace Notrelix.Application.Features.WorkManagement.BoardViews.Commands.SaveBoardView;

public record SaveBoardViewCommand(
    Guid WorkspaceId,
    Guid BoardId,
    ViewMode ViewMode,
    string? Filters) : ICommand<Result>, ITransactionalRequest, IRequirePermission, IWorkspaceRequest, IRealtimeRequest
{
    public PermissionAction Action => PermissionAction.ViewBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.Board, BoardId, WorkspaceId);
    public RealtimeTopic Topic => new("board", "Board", BoardId);
}

public class SaveBoardViewCommandHandler : IRequestHandler<SaveBoardViewCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public SaveBoardViewCommandHandler(
        IApplicationDbContext context,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    private static ViewType MapViewModeToViewType(ViewMode viewMode) => viewMode switch
    {
        ViewMode.Kanban => ViewType.Kanban,
        ViewMode.Calendar => ViewType.Calendar,
        ViewMode.Timeline => ViewType.Timeline,
        ViewMode.Table or ViewMode.List => ViewType.Table,
        _ => ViewType.Table
    };

    public async Task<Result> Handle(SaveBoardViewCommand request, CancellationToken ct)
    {
        var boardExists = await _context.Boards
            .AsNoTracking()
            .AnyAsync(board => board.Id == request.BoardId && !board.IsArchived, ct);
        if (!boardExists) throw new NotFoundException(nameof(BoardEntity), request.BoardId);

        var viewType = MapViewModeToViewType(request.ViewMode);
        var now = _dateTimeProvider.UtcNow;
        var config = BoardViewConfig.Create(JsonValue.Create(request.Filters ?? "{}"));

        var view = await _context.BoardViews
            .FirstOrDefaultAsync(v => v.BoardId == request.BoardId, ct);

        if (view is not null)
        {
            view.UpdateConfig(config, _currentUser.UserId, now);
        }
        else
        {
            view = BoardView.Create(
                request.WorkspaceId,
                request.BoardId,
                viewType.ToString(),
                viewType,
                config,
                _currentUser.UserId,
                now);
            _context.BoardViews.Add(view);
        }

        return Result.Success();
    }
}
