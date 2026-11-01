using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

using Notrelix.Application.Features.WorkManagement.Ordering;

namespace Notrelix.Application.Features.WorkManagement.BoardGroups.Commands.CreateBoardGroup;

[IdempotencyOperation("work-management.board-groups.create-board-group.v1")]
public record CreateBoardGroupCommand(Guid BoardId, string Title, string? Color = null, Guid? PreviousSiblingId = null, Guid? NextSiblingId = null) : ICommand<Result<Guid>>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    // Adding a group edits Board content (Editor), not Board management.
    public PermissionAction Action => PermissionAction.CreateItem;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board"), BoardId);
}

public class CreateBoardGroupCommandHandler : IRequestHandler<CreateBoardGroupCommand, Result<Guid>>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly WorkPlacementResolver _placement;
    private readonly IBoardRealtimeNotifier? _realtime;

    public CreateBoardGroupCommandHandler(
        IWorkManagementDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider,
        WorkPlacementResolver placement,
        IBoardRealtimeNotifier? realtime = null)
    {
        _realtime = realtime;
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
        _placement = placement;
    }

    public async Task<Result<Guid>> Handle(CreateBoardGroupCommand request, CancellationToken ct)
    {
        var response = await HandleCore(request, ct);
        if (_realtime is not null) await _realtime.NotifyAsync(request, response, ct);
        return response;
    }

    private async Task<Result<Guid>> HandleCore(CreateBoardGroupCommand request, CancellationToken ct)
    {
        var board = await _context.Boards
            .FirstOrDefaultAsync(board => board.Id == request.BoardId && !board.IsArchived, ct);
        if (board is null) throw new NotFoundException("Board", request.BoardId);

        var position = await _placement.ResolveAsync(
            new WorkOrderingScope(WorkOrderingScopeKind.BoardGroups, board.Id),
            new WorkPlacementIntent(request.PreviousSiblingId, request.NextSiblingId),
            token => BoardGroupSiblings.LoadAsync(_context, board.Id, token),
            ct);

        var color = request.Color is not null ? Color.Create(request.Color) : Color.Create("#808080");
        var list = BoardGroup.Create(_requestContext.RequireAccountId(), board.WorkspaceId, request.BoardId, request.Title, color, position, _requestContext.UserId, _dateTimeProvider.UtcNow);
        _context.BoardGroups.Add(list);
        return Result<Guid>.Success(list.Id);
    }
}
