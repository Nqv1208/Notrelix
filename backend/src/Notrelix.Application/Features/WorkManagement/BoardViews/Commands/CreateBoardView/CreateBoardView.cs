using Notrelix.Application.Features.WorkManagement.Common.DTOs;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.BoardViews.Commands.CreateBoardView;

[IdempotencyOperation("work-management.board-views.create-board-view.v1")]
public record CreateBoardViewCommand(
    Guid BoardId,
    string Name,
    string ViewMode,
    string ConfigJson,
    string? Visibility = null) : ICommand<BoardViewDto>, IWriteRequest, IRequirePermission, IAuthenticatedRequest, IResourceScopedRequest, IIdempotentRequest
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
        var visibility = BoardViewVisibility.Board;
        if (!string.IsNullOrWhiteSpace(request.Visibility) && !Enum.TryParse(request.Visibility, true, out visibility))
            throw new ArgumentException($"Invalid view visibility: {request.Visibility}");
        var view = BoardView.Create(_requestContext.RequireAccountId(), _requestContext.RequireWorkspaceId(), request.BoardId, request.Name, type, config, _requestContext.UserId, _dateTimeProvider.UtcNow, visibility: visibility);

        _context.BoardViews.Add(view);

        var response = BoardViewDto.From(view);
        if (_realtime is not null && _events is not null)
            _events.Add(_realtime.Map(request, response, view.Version));
        return response;
    }
}

/// <summary>A missing or unknown view mode or visibility is a 400 with the field named, not a server error.</summary>
public sealed class CreateBoardViewCommandValidator : AbstractValidator<CreateBoardViewCommand>
{
    public CreateBoardViewCommandValidator()
    {
        RuleFor(x => x.ViewMode)
            .Must(mode => Enum.TryParse<ViewType>(mode, true, out var type) && Enum.IsDefined(type))
            .WithMessage("'ViewMode' must be a known view type.");
        RuleFor(x => x.Visibility)
            .Must(visibility => string.IsNullOrWhiteSpace(visibility)
                || (Enum.TryParse<BoardViewVisibility>(visibility, true, out var parsed) && Enum.IsDefined(parsed)))
            .WithMessage("'Visibility' must be a known view visibility.");
    }
}

public sealed class CreateBoardViewRealtimeMapper(IExecutionContextReader context, IDateTimeProvider time)
    : RealtimeChangeMapper<CreateBoardViewCommand, BoardViewDto>(context, time)
{
    public override RealtimeResourceChangedV1 Map(CreateBoardViewCommand request, BoardViewDto response, long streamVersion) =>
        Create("board", "Board", request.BoardId, "CreateBoardView", response, streamVersion);
}
