using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Governance.Public.Visibility;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.AssignBoardItemMember;

[IdempotencyOperation("work-management.board-items.assign-board-item-member.v1")]
public record AssignBoardItemMemberCommand(
    Guid BoardItemId,
    Guid UserId) : ICommand<Result>, IWriteRequest, IRequirePermission, IAuthenticatedRequest, IResourceScopedRequest, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.AssignItem;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board-item"), BoardItemId);
}

public class AssignBoardItemMemberCommandHandler : IRequestHandler<AssignBoardItemMemberCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IWorkspaceAccessResolver _workspaceAccess;
    private readonly IResourceVisibility _visibility;
    private readonly IRealtimeChangeMapper<AssignBoardItemMemberCommand, Result>? _realtime;
    private readonly IIntegrationEventCollector? _events;

    public AssignBoardItemMemberCommandHandler(
        IWorkManagementDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider,
        IWorkspaceAccessResolver workspaceAccess,
        IResourceVisibility visibility,
        IRealtimeChangeMapper<AssignBoardItemMemberCommand, Result>? realtime = null,
        IIntegrationEventCollector? events = null)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
        _workspaceAccess = workspaceAccess;
        _visibility = visibility;
        _realtime = realtime;
        _events = events;
    }

    public async Task<Result> Handle(AssignBoardItemMemberCommand request, CancellationToken ct)
    {
        var card = await _context.BoardItems
            .FirstOrDefaultAsync(c => c.Id == request.BoardItemId, ct);
        if (card is null) throw new NotFoundException(nameof(BoardItem), request.BoardItemId);

        var access = await _workspaceAccess.ResolveAsync(_requestContext.RequireWorkspaceId(), request.UserId, ct);
        if (!access.CanAccess)
            throw new ForbiddenException("Chỉ có thể assign thành viên thuộc cùng workspace.");

        // WM-040: assignment requires access at the time it is made. The assignee
        // must be able to ViewBoard on the Item's Board (the same decision a single
        // read makes); the caller may share the Board with them first.
        var visibleBoards = await _visibility.VisibleBoardIdsAsync(card.WorkspaceId, request.UserId, ct);
        if (!visibleBoards.Contains(card.BoardId))
            throw new ForbiddenException("Người này chưa xem được board của việc, nên chưa thể giao việc. Hãy chia sẻ board với họ trước.");

        var alreadyAssigned = await _context.BoardItemMembers
            .AnyAsync(m => m.ItemId == card.Id && m.UserId == request.UserId, ct);
        if (alreadyAssigned) return Result.Success();

        var member = BoardItemMember.Create(
            _requestContext.RequireAccountId(),
            card.WorkspaceId,
            card.BoardId,
            card.Id,
            request.UserId,
            _requestContext.UserId,
            _dateTimeProvider.UtcNow);

        _context.BoardItemMembers.Add(member);
        var response = Result.Success();
        if (_realtime is not null && _events is not null)
            _events.Add(_realtime.Map(request, response, card.Version));
        return response;
    }
}

public sealed class AssignBoardItemMemberRealtimeMapper(IExecutionContextReader context, IDateTimeProvider time)
    : RealtimeChangeMapper<AssignBoardItemMemberCommand, Result>(context, time)
{
    public override RealtimeResourceChangedV1 Map(AssignBoardItemMemberCommand request, Result response, long streamVersion) =>
        Create("board", "BoardItem", request.BoardItemId, "AssignBoardItemMember", response, streamVersion);
}
