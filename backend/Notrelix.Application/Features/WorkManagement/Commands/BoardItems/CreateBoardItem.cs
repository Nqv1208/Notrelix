using MediatR;
using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Security;
using Notrelix.Application.Features.WorkManagement.DTOs;

namespace Notrelix.Application.Features.WorkManagement.Commands;

public record CreateBoardItemCommand(
    Guid WorkspaceId,
    Guid BoardId,
    Guid GroupId,
    string Title,
    double Position) : IRequest<BoardItemSlimDto>, IAuthorizeableRequest
{
    public ResourceType ResourceType => ResourceType.Board;
    public Guid ResourceId => BoardId;
    public PermissionAction Action => PermissionAction.CreateItem;
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
        await _context.SaveChangesAsync(cancellationToken);

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
