using BoardEntity = global::Notrelix.Domain.WorkManagement.Boards.Board;
using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.BoardViews.Commands.SaveBoardView;

[IdempotencyOperation("work-management.board-views.save-board-view.v1")]
public record SaveBoardViewCommand(
    Guid BoardId,
    ViewMode ViewMode,
    string? Filters,
    long? ExpectedVersion = null) : ICommand<Result>, IWriteRequest, IRequirePermission, IAuthenticatedRequest, IResourceScopedRequest, IIdempotentRequest
{
    // Saves the Board's shared view, changed for everyone: an Editor change.
    public PermissionAction Action => PermissionAction.UpdateBoardView;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board"), BoardId);
}

public class SaveBoardViewCommandHandler : IRequestHandler<SaveBoardViewCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IRealtimeChangeMapper<SaveBoardViewCommand, Result>? _realtime;
    private readonly IIntegrationEventCollector? _events;

    public SaveBoardViewCommandHandler(
        IWorkManagementDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider,
        IRealtimeChangeMapper<SaveBoardViewCommand, Result>? realtime = null,
        IIntegrationEventCollector? events = null)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
        _realtime = realtime;
        _events = events;
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
            .Where(v => v.BoardId == request.BoardId && v.DeletedAt == null && v.Visibility == BoardViewVisibility.Board)
            .OrderByDescending(v => v.IsDefault)
            .ThenBy(v => v.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (view is not null)
        {
            // The view is found here, not named by the request, so the pipeline's version guard cannot bind to it;
            // the version is checked on the tracked view and the row's version token still guards the write.
            // Fails closed: overwriting an existing shared view without naming the version read is refused.
            if (request.ExpectedVersion != view.Version)
                throw new PreconditionFailedException(
                    "The view was modified by another request. Reload and retry.", "common.precondition-failed", request.ExpectedVersion, view.Version);
            var before = view.Version;
            view.UpdateConfig(config, _requestContext.UserId, now);
            // A save that changed nothing bumps no version and tells no one.
            if (view.Version == before) return Result.Success();
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

        var response = Result.Success();
        if (_realtime is not null && _events is not null)
            _events.Add(_realtime.Map(request, response, view.Version));
        return response;
    }
}

public sealed class SaveBoardViewRealtimeMapper(IExecutionContextReader context, IDateTimeProvider time)
    : RealtimeChangeMapper<SaveBoardViewCommand, Result>(context, time)
{
    public override RealtimeResourceChangedV1 Map(SaveBoardViewCommand request, Result response, long streamVersion) =>
        Create("board", "Board", request.BoardId, "SaveBoardView", response, streamVersion);
}
