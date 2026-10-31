using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Governance.Public.Visibility;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Boards.Services;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.AssignBoardItemMember;

[IdempotencyOperation("work-management.board-items.assign-board-item-member.v1")]
public record AssignBoardItemMemberCommand(
    Guid BoardItemId,
    Guid UserId,
    long ExpectedVersion) : ICommand<Result>, IWriteRequest, IRequirePermission, IAuthenticatedRequest, IResourceScopedRequest, IIdempotentRequest, IExpectedVersionRequest
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
    private readonly IBoardRealtimeNotifier? _realtime;

    public AssignBoardItemMemberCommandHandler(
        IWorkManagementDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider,
        IWorkspaceAccessResolver workspaceAccess,
        IResourceVisibility visibility,
        IBoardRealtimeNotifier? realtime = null)
    {
        _realtime = realtime;
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
        _workspaceAccess = workspaceAccess;
        _visibility = visibility;
    }

    public async Task<Result> Handle(AssignBoardItemMemberCommand request, CancellationToken ct)
    {
        var response = await HandleCore(request, ct);
        if (_realtime is not null) await _realtime.NotifyAsync(request, response, ct);
        return response;
    }

    private async Task<Result> HandleCore(AssignBoardItemMemberCommand request, CancellationToken ct)
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

        await BoardAssigneeGuard.EnsureNotViewerAsync(_context, card.BoardId, request.UserId, ct);

        var alreadyAssigned = await _context.BoardItemMembers
            .AnyAsync(m => m.ItemId == card.Id && m.UserId == request.UserId, ct);
        if (alreadyAssigned) return VersionedResult.Success(card.Version);

        var member = BoardItemMember.Create(
            _requestContext.RequireAccountId(),
            card.WorkspaceId,
            card.BoardId,
            card.Id,
            request.UserId,
            _requestContext.UserId,
            _dateTimeProvider.UtcNow);

        _context.BoardItemMembers.Add(member);
        card.NoteAssigneesChanged(_requestContext.UserId, _dateTimeProvider.UtcNow);
        var response = VersionedResult.Success(card.Version);
        return response;
    }
}
