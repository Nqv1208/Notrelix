using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UnassignBoardItemMember;

[IdempotencyOperation("work-management.board-items.unassign-board-item-member.v1")]
public record UnassignBoardItemMemberCommand(Guid BoardItemId, Guid UserId) : ICommand<Result>, ITransactionalRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.AssignItem;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board-item"), BoardItemId);
}

public class UnassignBoardItemMemberCommandHandler : IRequestHandler<UnassignBoardItemMemberCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    public UnassignBoardItemMemberCommandHandler(IWorkManagementDbContext context) => _context = context;

    public async Task<Result> Handle(UnassignBoardItemMemberCommand request, CancellationToken ct)
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
