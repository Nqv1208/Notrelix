using Notrelix.Application.Features.WorkManagement.Common.DTOs;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.BoardViews.Commands.CreateBoardView;

[IdempotencyOperation("work-management.board-views.create-board-view.v1")]
public record CreateBoardViewCommand(
    Guid BoardId,
    string Name,
    string ViewMode,
    string ConfigJson) : ICommand<BoardViewDto>, IWriteRequest, IRequirePermission, IAuthenticatedRequest, IResourceScopedRequest, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.CreateBoardView;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board"), BoardId);
}

public class CreateBoardViewCommandHandler : IRequestHandler<CreateBoardViewCommand, BoardViewDto>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IRealtimeChangeMapper<CreateBoardViewCommand, BoardViewDto>? _realtime;
    private readonly IIntegrationEventCollector? _events;

    public CreateBoardViewCommandHandler(IWorkManagementDbContext context, ICurrentRequestContext requestContext, IDateTimeProvider dateTimeProvider, IRealtimeChangeMapper<CreateBoardViewCommand, BoardViewDto>? realtime = null, IIntegrationEventCollector? events = null)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
        _realtime = realtime;
        _events = events;
    }

    public async Task<BoardViewDto> Handle(CreateBoardViewCommand request, CancellationToken cancellationToken)
    {
        var board = await _context.Boards
            .FirstOrDefaultAsync(b => b.Id == request.BoardId, cancellationToken);

        if (board == null)
            throw new NotFoundException("Board", request.BoardId);

        if (!Enum.TryParse<ViewType>(request.ViewMode, true, out var type))
        {
            throw new ArgumentException($"Invalid view mode: {request.ViewMode}");
        }

        var configData = JsonValue.Create(request.ConfigJson);
        var config = BoardViewConfig.Create(configData);
        var view = BoardView.Create(_requestContext.RequireAccountId(), _requestContext.RequireWorkspaceId(), request.BoardId, request.Name, type, config, _requestContext.UserId, _dateTimeProvider.UtcNow);

        _context.BoardViews.Add(view);

        var response = new BoardViewDto(
            view.Id,
            view.BoardId,
            view.Name,
            view.Type.ToString(),
            view.Config.Data.Value,
            view.IsDefault
        );
        if (_realtime is not null && _events is not null)
            _events.Add(_realtime.Map(request, response, view.Version));
        return response;
    }
}

public sealed class CreateBoardViewRealtimeMapper(IExecutionContextReader context, IDateTimeProvider time)
    : RealtimeChangeMapper<CreateBoardViewCommand, BoardViewDto>(context, time)
{
    public override RealtimeResourceChangedV1 Map(CreateBoardViewCommand request, BoardViewDto response, long streamVersion) =>
        Create("board", "Board", request.BoardId, "CreateBoardView", response, streamVersion);
}
