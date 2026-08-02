using BoardEntity = global::Notrelix.Domain.WorkManagement.Boards.Board;
using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Common.Idempotency;

namespace Notrelix.Application.Features.WorkManagement.BoardViews.Commands.SaveBoardView;

[IdempotencyOperation("work-management.board-views.save-board-view.v1")]
public record SaveBoardViewCommand(
    Guid BoardId,
    ViewMode ViewMode,
    string? Filters,
    string? IdempotencyKey = null) : ICommand<Result>, ITransactionalRequest, IRequirePermission, IResourceScopedRequest, IRealtimeRequest, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.ViewBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.Board, BoardId);
    public RealtimeTopic Topic => new("board", "Board", BoardId);
    string IIdempotentRequest.IdempotencyKey => IdempotencyKey ?? $"save-view:{BoardId}";
}

public class SaveBoardViewCommandHandler : IRequestHandler<SaveBoardViewCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public SaveBoardViewCommandHandler(
        IWorkManagementDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _requestContext = requestContext;
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
        var board = await _context.Boards
            .AsNoTracking()
            .FirstOrDefaultAsync(board => board.Id == request.BoardId && !board.IsArchived, ct);
        if (board is null) throw new NotFoundException(nameof(BoardEntity), request.BoardId);

        var viewType = MapViewModeToViewType(request.ViewMode);
        var now = _dateTimeProvider.UtcNow;
        var config = BoardViewConfig.Create(JsonValue.Create(request.Filters ?? "{}"));

        var view = await _context.BoardViews
            .FirstOrDefaultAsync(v => v.BoardId == request.BoardId, ct);

        if (view is not null)
        {
            view.UpdateConfig(config, _requestContext.UserId, now);
        }
        else
        {
            view = BoardView.Create(
                _requestContext.RequireAccountId(),
                board.WorkspaceId,
                request.BoardId,
                viewType.ToString(),
                viewType,
                config,
                _requestContext.UserId,
                now);
            _context.BoardViews.Add(view);
        }

        return Result.Success();
    }
}
