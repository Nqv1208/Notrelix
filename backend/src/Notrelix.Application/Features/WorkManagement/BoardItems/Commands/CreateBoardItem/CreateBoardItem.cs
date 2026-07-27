using Notrelix.Application.Features.WorkManagement.Common.DTOs;
using Notrelix.Application.Features.WorkManagement.Abstractions;

using Notrelix.Domain.SharedKernel.Ordering;
namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.CreateBoardItem;

public record CreateBoardItemCommand(
    Guid BoardId,
    Guid GroupId,
    string Title,
    double Position,
    string? IdempotencyKey = null) : ICommand<BoardItemSlimDto>, ITransactionalRequest, IRequirePermission, IResourceScopedRequest, IRealtimeRequest, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.CreateItem;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.Board, BoardId);
    public RealtimeTopic Topic => new("board", "Board", BoardId);
    string IIdempotentRequest.IdempotencyKey => IdempotencyKey ?? $"create-item:{BoardId}:{GroupId}:{Title}";
}

public class CreateBoardItemCommandHandler : IRequestHandler<CreateBoardItemCommand, BoardItemSlimDto>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _timeProvider;

    public CreateBoardItemCommandHandler(IWorkManagementDbContext context, ICurrentRequestContext requestContext, IDateTimeProvider timeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _timeProvider = timeProvider;
    }

    public async Task<BoardItemSlimDto> Handle(CreateBoardItemCommand request, CancellationToken cancellationToken)
    {
        var group = await _context.BoardGroups
            .FirstOrDefaultAsync(g => g.Id == request.GroupId && g.BoardId == request.BoardId, cancellationToken);

        if (group == null)
            throw new NotFoundException("BoardGroup", request.GroupId);

        var now = _timeProvider.UtcNow;
        var position = FractionalIndexGenerator.GenerateKeyBetween(null, null);

        var item = BoardItem.Create(
            _requestContext.RequireAccountId(),
            _requestContext.RequireWorkspaceId(),
            request.BoardId,
            request.GroupId,
            request.Title,
            position,
            _requestContext.UserId,
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
