using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UnarchiveBoardItem;

[IdempotencyOperation("work-management.board-items.unarchive-board-item.v1")]
public record UnarchiveBoardItemCommand(Guid BoardItemId) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.UpdateItem;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board-item"), BoardItemId);
}

public class UnarchiveBoardItemCommandHandler : IRequestHandler<UnarchiveBoardItemCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _timeProvider;

    public UnarchiveBoardItemCommandHandler(
        IWorkManagementDbContext context,
        ICurrentUser currentUser,
        IDateTimeProvider timeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result> Handle(UnarchiveBoardItemCommand request, CancellationToken ct)
    {
        var item = await _context.BoardItems
            .FirstOrDefaultAsync(i => i.Id == request.BoardItemId, ct);
        if (item is null) throw new NotFoundException(nameof(BoardItem), request.BoardItemId);

        item.Unarchive(_currentUser.UserId, _timeProvider.UtcNow);
        return Result.Success();
    }
}
