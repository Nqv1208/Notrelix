using Notrelix.Application.Features.WorkManagement.Common.DTOs;
using Notrelix.Application.Features.WorkManagement.Abstractions;

using Notrelix.Domain.SharedKernel.Ordering;
namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.MoveBoardItem;

public record MoveBoardItemCommand(
    Guid ItemId,
    Guid NewGroupId,
    double Position,
    string? IdempotencyKey = null) : ICommand<BoardItemSlimDto>, ITransactionalRequest, IRequirePermission, IResourceScopedRequest, IRealtimeRequest, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.MoveItem;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.BoardItem, ItemId);
    public RealtimeTopic Topic => new("board", "Board", ItemId);
    string IIdempotentRequest.IdempotencyKey => IdempotencyKey ?? $"move-item:{ItemId}";
}

public class MoveBoardItemCommandHandler : IRequestHandler<MoveBoardItemCommand, BoardItemSlimDto>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _timeProvider;

    public MoveBoardItemCommandHandler(IWorkManagementDbContext context, ICurrentUser currentUser, IDateTimeProvider timeProvider)
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
            .FirstOrDefaultAsync(g => g.Id == request.NewGroupId && g.BoardId == item.BoardId, cancellationToken);

        if (group == null)
            throw new NotFoundException("BoardGroup", request.NewGroupId);

        var now = _timeProvider.UtcNow;
        var position = FractionalIndexGenerator.GenerateKeyBetween(null, null);

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
