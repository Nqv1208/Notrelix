using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.DeleteBoardItem;

[IdempotencyOperation("work-management.board-items.delete-board-item.v1")]
public record DeleteBoardItemCommand(Guid BoardItemId, long ExpectedVersion) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest, IExpectedVersionRequest
{
    public PermissionAction Action => PermissionAction.UpdateItem;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board-item"), BoardItemId);
}

public class DeleteBoardItemCommandHandler : IRequestHandler<DeleteBoardItemCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _timeProvider;

    public DeleteBoardItemCommandHandler(
        IWorkManagementDbContext context,
        ICurrentUser currentUser,
        IDateTimeProvider timeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result> Handle(DeleteBoardItemCommand request, CancellationToken ct)
    {
        var item = await _context.BoardItems
            .FirstOrDefaultAsync(i => i.Id == request.BoardItemId, ct);
        if (item is null) throw new NotFoundException(nameof(BoardItem), request.BoardItemId);

        var now = _timeProvider.UtcNow;
        item.Delete(_currentUser.UserId, now);

        // A parent takes its sub-items to the Trash with it, at the same instant, so a restore can bring back
        // exactly those and not a sub-item deleted on its own earlier.
        var children = await _context.BoardItems
            .Where(child => child.ParentItemId == item.Id && child.DeletedAt == null)
            .ToListAsync(ct);
        foreach (var child in children) child.Delete(_currentUser.UserId, now);
        return Result.Success();
    }
}
