using Notrelix.Application.Features.WorkManagement.Common.DTOs;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.BoardViews.Commands.CreateBoardView;

public record CreateBoardViewCommand(
    Guid WorkspaceId,
    Guid BoardId,
    string Name,
    string ViewMode,
    string ConfigJson) : ICommand<BoardViewDto>, ITransactionalRequest, IRequirePermission, IWorkspaceRequest, IRealtimeRequest
{
    public PermissionAction Action => PermissionAction.CreateBoardView;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.Board, BoardId, WorkspaceId);
    public RealtimeTopic Topic => new("board", "Board", BoardId);
}

public class CreateBoardViewCommandHandler : IRequestHandler<CreateBoardViewCommand, BoardViewDto>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICurrentTenantContext _tenant;

    public CreateBoardViewCommandHandler(IWorkManagementDbContext context, ICurrentUser currentUser, IDateTimeProvider dateTimeProvider, ICurrentTenantContext tenant)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
        _tenant = tenant;
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
        var view = BoardView.Create(_tenant.RequireAccountId(), request.WorkspaceId, request.BoardId, request.Name, type, config, _currentUser.UserId, _dateTimeProvider.UtcNow);

        _context.BoardViews.Add(view);

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
