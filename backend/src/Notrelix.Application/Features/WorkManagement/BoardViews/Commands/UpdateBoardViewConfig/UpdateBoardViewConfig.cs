using Notrelix.Application.Features.WorkManagement.Common.DTOs;

namespace Notrelix.Application.Features.WorkManagement.BoardViews.Commands.UpdateBoardViewConfig;

public record UpdateBoardViewConfigCommand(
    Guid WorkspaceId,
    Guid BoardId,
    Guid ViewId,
    string ConfigJson) : ICommand<BoardViewDto>, ITransactionalRequest, IRequirePermission, IWorkspaceRequest, IRealtimeRequest
{
    public PermissionAction Action => PermissionAction.UpdateBoardView;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.Board, BoardId, WorkspaceId);
    public RealtimeTopic Topic => new("board", "Board", BoardId);
}

public class UpdateBoardViewConfigCommandHandler : IRequestHandler<UpdateBoardViewConfigCommand, BoardViewDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateBoardViewConfigCommandHandler(IApplicationDbContext context, ICurrentUser currentUser, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<BoardViewDto> Handle(UpdateBoardViewConfigCommand request, CancellationToken cancellationToken)
    {
        var view = await _context.BoardViews
            .FirstOrDefaultAsync(v => v.Id == request.ViewId && v.BoardId == request.BoardId, cancellationToken);

        if (view == null)
            throw new NotFoundException("BoardView", request.ViewId);

        var config = BoardViewConfig.Create(JsonValue.Create(request.ConfigJson));
        view.UpdateConfig(config, _currentUser.UserId, _dateTimeProvider.UtcNow);

        return new BoardViewDto(
            view.Id,
            view.BoardId,
            view.Name,
            view.Type.ToString(),
            view.Config.Data.Value,
            view.IsDefault
        );
    }
}
