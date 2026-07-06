using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.AssignBoardItemMember;

public record AssignBoardItemMemberCommand(
    Guid BoardItemId,
    Guid UserId) : ICommand<Result>, ITransactionalRequest, IRequirePermission, IResourceScopedRequest, IRealtimeRequest
{
    public PermissionAction Action => PermissionAction.AssignItem;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.BoardItem, BoardItemId);
    public RealtimeTopic Topic => new("board", "BoardItem", BoardItemId);
}

public class AssignBoardItemMemberCommandHandler : IRequestHandler<AssignBoardItemMemberCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IWorkspaceAccessResolver _workspaceAccess;
    private readonly ICurrentTenantContext _tenant;

    public AssignBoardItemMemberCommandHandler(
        IWorkManagementDbContext context,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider,
        IWorkspaceAccessResolver workspaceAccess,
        ICurrentTenantContext tenant)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
        _workspaceAccess = workspaceAccess;
        _tenant = tenant;
    }

    public async Task<Result> Handle(AssignBoardItemMemberCommand request, CancellationToken ct)
    {
        var card = await _context.BoardItems
            .FirstOrDefaultAsync(c => c.Id == request.BoardItemId, ct);
        if (card is null) throw new NotFoundException(nameof(BoardItem), request.BoardItemId);

        var access = await _workspaceAccess.ResolveAsync(_tenant.RequireWorkspaceId(), request.UserId, ct);
        if (!access.CanAccess)
            throw new ForbiddenException("Chỉ có thể assign thành viên thuộc cùng workspace.");

        var alreadyAssigned = await _context.BoardItemMembers
            .AnyAsync(m => m.ItemId == card.Id && m.UserId == request.UserId, ct);
        if (alreadyAssigned) return Result.Success();

        var member = BoardItemMember.Create(
            _tenant.RequireAccountId(),
            card.WorkspaceId,
            card.BoardId,
            card.Id,
            request.UserId,
            _currentUser.UserId,
            _dateTimeProvider.UtcNow);

        _context.BoardItemMembers.Add(member);
        return Result.Success();
    }
}
