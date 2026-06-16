using MediatR;
using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.Security;
using Notrelix.Application.Features.WorkManagement.DTOs;

namespace Notrelix.Application.Features.WorkManagement.Commands;

public record CreateBoardViewCommand(
    Guid WorkspaceId,
    Guid BoardId,
    string Name,
    string ViewMode,
    string ConfigJson) : IRequest<BoardViewDto>, IAuthorizeableRequest
{
    public ResourceType ResourceType => ResourceType.Board;
    public Guid ResourceId => BoardId;
    public PermissionAction Action => PermissionAction.CreateBoardView;
}

public class CreateBoardViewCommandHandler : IRequestHandler<CreateBoardViewCommand, BoardViewDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateBoardViewCommandHandler(IApplicationDbContext context, ICurrentUser currentUser, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUser = currentUser;
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
        var view = BoardView.Create(request.WorkspaceId, request.BoardId, request.Name, type, config, _currentUser.UserId, _dateTimeProvider.UtcNow);

        _context.BoardViews.Add(view);
        await _context.SaveChangesAsync(cancellationToken);

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
