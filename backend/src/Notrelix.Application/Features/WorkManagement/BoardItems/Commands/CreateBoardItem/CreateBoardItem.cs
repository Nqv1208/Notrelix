using MediatR;
using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Features.WorkManagement.Common.DTOs;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.CreateBoardItem;

public record CreateBoardItemCommand(
    Guid WorkspaceId,
    Guid BoardId,
    Guid GroupId,
    string Title,
    double Position) : ICommand<BoardItemSlimDto>, ITransactionalRequest, IRequirePermission, IWorkspaceRequest, IRealtimeRequest
{
    public PermissionAction Action => PermissionAction.CreateItem;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.Board, BoardId, WorkspaceId);
    public RealtimeTopic Topic => new("board", "Board", BoardId);
}

public class CreateBoardItemCommandHandler : IRequestHandler<CreateBoardItemCommand, BoardItemSlimDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _timeProvider;

    public CreateBoardItemCommandHandler(IApplicationDbContext context, ICurrentUser currentUser, IDateTimeProvider timeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<BoardItemSlimDto> Handle(CreateBoardItemCommand request, CancellationToken cancellationToken)
    {
        var group = await _context.BoardGroups
            .FirstOrDefaultAsync(g => g.Id == request.GroupId && g.BoardId == request.BoardId, cancellationToken);

        if (group == null)
            throw new NotFoundException("BoardGroup", request.GroupId);

        var now = _timeProvider.UtcNow;
        var position = FractionalIndex.Create(request.Position.ToString(System.Globalization.CultureInfo.InvariantCulture));

        var item = BoardItem.Create(
            request.WorkspaceId,
            request.BoardId,
            request.GroupId,
            request.Title,
            position,
            _currentUser.UserId,
            now);

        _context.BoardItems.Add(item);

        return new BoardItemSlimDto(
            item.Id,
            item.GroupId,
            item.Name,
            item.Position.Value,
            new List<Guid>(),
            new List<Guid>()
        );
    }
}
