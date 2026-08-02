using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UnarchiveBoardItem;

[IdempotencyOperation("work-management.board-items.unarchive-board-item.v1")]
public record UnarchiveBoardItemCommand(Guid BoardItemId, string? IdempotencyKey = null) : ICommand<Result>, ITransactionalRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.UpdateItem;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.BoardItem, BoardItemId);
    string IIdempotentRequest.IdempotencyKey => IdempotencyKey ?? $"unarchive-item:{BoardItemId}";
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
