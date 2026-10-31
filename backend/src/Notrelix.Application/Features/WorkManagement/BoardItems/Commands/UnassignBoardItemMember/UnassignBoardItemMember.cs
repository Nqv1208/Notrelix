using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UnassignBoardItemMember;

[IdempotencyOperation("work-management.board-items.unassign-board-item-member.v1")]
public record UnassignBoardItemMemberCommand(Guid BoardItemId, Guid UserId) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.AssignItem;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board-item"), BoardItemId);
}

public class UnassignBoardItemMemberCommandHandler : IRequestHandler<UnassignBoardItemMemberCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly IBoardRealtimeNotifier? _realtime;

    public UnassignBoardItemMemberCommandHandler(IWorkManagementDbContext context, IBoardRealtimeNotifier? realtime = null)
    {
        _context = context;
        _realtime = realtime;
    }

    public async Task<Result> Handle(UnassignBoardItemMemberCommand request, CancellationToken ct)
    {
        var response = await HandleCore(request, ct);
        if (_realtime is not null) await _realtime.NotifyAsync(request, response, ct);
        return response;
    }

    private async Task<Result> HandleCore(UnassignBoardItemMemberCommand request, CancellationToken ct)
    {
        var member = await _context.BoardItemMembers
            .FirstOrDefaultAsync(m => m.ItemId == request.BoardItemId && m.UserId == request.UserId, ct);
        if (member is not null)
        {
            _context.BoardItemMembers.Remove(member);
        }
        return Result.Success();
    }
}
