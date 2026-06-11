using MediatR;
using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.Security;
using Notrelix.Application.Features.WorkManagement.DTOs;
using Notrelix.Domain.WorkManagement;

namespace Notrelix.Application.Features.WorkManagement.Commands;

public record CreateBoardViewCommand(
    Guid WorkspaceId,
    Guid BoardId,
    string Name,
    string ViewMode,
    double Position) : IRequest<BoardViewDto>, IAuthorizeableRequest
{
    public ResourceType ResourceType => ResourceType.Board;
    public Guid ResourceId => BoardId;
    public PermissionAction Action => PermissionAction.CreateBoardView;
}

public class CreateBoardViewCommandHandler : IRequestHandler<CreateBoardViewCommand, BoardViewDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;

    public CreateBoardViewCommandHandler(IApplicationDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<BoardViewDto> Handle(CreateBoardViewCommand request, CancellationToken cancellationToken)
    {
        var board = await _context.Boards
            .FirstOrDefaultAsync(b => b.Id == request.BoardId, cancellationToken);

        if (board == null)
            throw new NotFoundException("Board", request.BoardId);

        if (!Enum.TryParse<ViewMode>(request.ViewMode, true, out var mode))
        {
            throw new ArgumentException($"Invalid view mode: {request.ViewMode}");
        }

        var view = BoardView.CreateSaved(request.WorkspaceId, request.BoardId, _currentUser.UserId, request.Name, mode, request.Position, isDefault: false, isPrivate: false);

        _context.BoardViews.Add(view);
        await _context.SaveChangesAsync(cancellationToken);

        return new BoardViewDto(
            view.Id,
            view.BoardId,
            view.Name,
            view.ViewMode.ToString(),
            view.Filters,
            view.Config,
            view.Position,
            view.IsDefault,
            view.IsPrivate
        );
    }
}
