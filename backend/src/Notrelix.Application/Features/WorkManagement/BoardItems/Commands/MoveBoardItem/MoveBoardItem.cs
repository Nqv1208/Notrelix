using MediatR;
using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Features.WorkManagement.Common.DTOs;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.MoveBoardItem;

public record MoveBoardItemCommand(
    Guid WorkspaceId,
    Guid BoardId,
    Guid ItemId,
    Guid NewGroupId,
    double Position) : ICommand<BoardItemSlimDto>, ITransactionalRequest, IRequirePermission, IWorkspaceRequest, IRealtimeRequest
{
    public PermissionAction Action => PermissionAction.MoveItem;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.Board, BoardId, WorkspaceId);
    public RealtimeTopic Topic => new("board", "Board", BoardId);
}

public class MoveBoardItemCommandHandler : IRequestHandler<MoveBoardItemCommand, BoardItemSlimDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _timeProvider;

    public MoveBoardItemCommandHandler(IApplicationDbContext context, ICurrentUser currentUser, IDateTimeProvider timeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<BoardItemSlimDto> Handle(MoveBoardItemCommand request, CancellationToken cancellationToken)
    {
        var item = await _context.BoardItems
            .FirstOrDefaultAsync(item => item.Id == request.ItemId, cancellationToken);

        if (item == null)
            throw new NotFoundException("BoardItem", request.ItemId);

        var group = await _context.BoardGroups
            .FirstOrDefaultAsync(g => g.Id == request.NewGroupId && g.BoardId == request.BoardId, cancellationToken);

        if (group == null)
            throw new NotFoundException("BoardGroup", request.NewGroupId);

        var now = _timeProvider.UtcNow;
        var position = FractionalIndex.Create(request.Position.ToString(System.Globalization.CultureInfo.InvariantCulture));

        item.MoveToGroup(BoardGroupRef.From(group), position, _currentUser.UserId, now);

        var memberIds = await _context.BoardItemMembers
            .Where(m => m.ItemId == item.Id)
            .Select(m => m.UserId)
            .ToListAsync(cancellationToken);

        var labelIds = await _context.BoardItemLabels
            .Where(l => l.ItemId == item.Id)
            .Select(l => l.LabelId)
            .ToListAsync(cancellationToken);

        return new BoardItemSlimDto(
            item.Id,
            item.GroupId,
            item.Name,
            item.Position.Value,
            memberIds,
            labelIds
        );
    }
}
