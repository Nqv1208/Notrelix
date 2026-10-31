using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UnassignBoardItemMember;

[IdempotencyOperation("work-management.board-items.unassign-board-item-member.v1")]
public record UnassignBoardItemMemberCommand(Guid BoardItemId, Guid UserId, long ExpectedVersion) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest, IExpectedVersionRequest
{
    public PermissionAction Action => PermissionAction.AssignItem;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board-item"), BoardItemId);
}

public class UnassignBoardItemMemberCommandHandler : IRequestHandler<UnassignBoardItemMemberCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _timeProvider;

    public UnassignBoardItemMemberCommandHandler(
        IWorkManagementDbContext context, ICurrentUser currentUser, IDateTimeProvider timeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result> Handle(UnassignBoardItemMemberCommand request, CancellationToken ct)
    {
        var item = await _context.BoardItems.FirstOrDefaultAsync(i => i.Id == request.BoardItemId, ct)
            ?? throw new NotFoundException(nameof(BoardItem), request.BoardItemId);
        var member = await _context.BoardItemMembers
            .FirstOrDefaultAsync(m => m.ItemId == request.BoardItemId && m.UserId == request.UserId, ct);
        if (member is not null)
        {
            _context.BoardItemMembers.Remove(member);
            item.NoteAssigneesChanged(_currentUser.UserId, _timeProvider.UtcNow);
        }
        return VersionedResult.Success(item.Version);
    }
}
