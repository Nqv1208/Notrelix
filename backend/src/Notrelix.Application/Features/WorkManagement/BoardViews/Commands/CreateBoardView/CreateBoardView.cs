using Notrelix.Application.Features.WorkManagement.Common.DTOs;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.BoardViews.Commands.CreateBoardView;

public record CreateBoardViewCommand(
    Guid BoardId,
    string Name,
    string ViewMode,
    string ConfigJson,
    string? IdempotencyKey = null) : ICommand<BoardViewDto>, ITransactionalRequest, IRequirePermission, IResourceScopedRequest, IRealtimeRequest, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.CreateBoardView;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.Board, BoardId);
    public RealtimeTopic Topic => new("board", "Board", BoardId);
    string IIdempotentRequest.IdempotencyKey => IdempotencyKey ?? $"create-view:{BoardId}:{Name}";
}

public class CreateBoardViewCommandHandler : IRequestHandler<CreateBoardViewCommand, BoardViewDto>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateBoardViewCommandHandler(IWorkManagementDbContext context, ICurrentRequestContext requestContext, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
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
