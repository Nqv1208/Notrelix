using Notrelix.Application.Features.WorkManagement.Common.DTOs;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.BoardViews.Commands.UpdateBoardViewConfig;

[IdempotencyOperation("work-management.board-views.update-board-view-config.v1")]
public record UpdateBoardViewConfigCommand(
    Guid BoardId,
    Guid ViewId,
    string ConfigJson) : ICommand<BoardViewDto>, IWriteRequest, IRequirePermission, IAuthenticatedRequest, IResourceScopedRequest, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.UpdateBoardView;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board-view"), ViewId);
}

public class UpdateBoardViewConfigCommandHandler : IRequestHandler<UpdateBoardViewConfigCommand, BoardViewDto>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IRealtimeChangeMapper<UpdateBoardViewConfigCommand, BoardViewDto>? _realtime;
    private readonly IIntegrationEventCollector? _events;

    public UpdateBoardViewConfigCommandHandler(IWorkManagementDbContext context, ICurrentUser currentUser, IDateTimeProvider dateTimeProvider, IRealtimeChangeMapper<UpdateBoardViewConfigCommand, BoardViewDto>? realtime = null, IIntegrationEventCollector? events = null)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
        _realtime = realtime;
        _events = events;
    }

    public async Task<BoardViewDto> Handle(UpdateBoardViewConfigCommand request, CancellationToken cancellationToken)
    {
        var view = await _context.BoardViews
            .FirstOrDefaultAsync(v => v.Id == request.ViewId && v.BoardId == request.BoardId, cancellationToken);

        if (view == null)
            throw new NotFoundException("BoardView", request.ViewId);

        var config = BoardViewConfig.Create(JsonValue.Create(request.ConfigJson));
        view.UpdateConfig(config, _currentUser.UserId, _dateTimeProvider.UtcNow);

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

public sealed class UpdateBoardViewConfigRealtimeMapper(IExecutionContextReader context, IDateTimeProvider time)
    : RealtimeChangeMapper<UpdateBoardViewConfigCommand, BoardViewDto>(context, time)
{
    public override RealtimeResourceChangedV1 Map(UpdateBoardViewConfigCommand request, BoardViewDto response, long streamVersion) =>
        Create("board", "Board", request.BoardId, "UpdateBoardViewConfig", response, streamVersion);
}
