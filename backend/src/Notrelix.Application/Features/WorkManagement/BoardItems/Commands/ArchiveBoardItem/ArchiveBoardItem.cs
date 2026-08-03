using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.ArchiveBoardItem;

[IdempotencyOperation("work-management.board-items.archive-board-item.v1")]
public record ArchiveBoardItemCommand(Guid BoardItemId) : ICommand<Result>, ITransactionalRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.UpdateItem;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.BoardItem, BoardItemId);
}

public class ArchiveBoardItemCommandHandler : IRequestHandler<ArchiveBoardItemCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _timeProvider;

    public ArchiveBoardItemCommandHandler(
        IWorkManagementDbContext context,
        ICurrentUser currentUser,
        IDateTimeProvider timeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result> Handle(ArchiveBoardItemCommand request, CancellationToken ct)
    {
        var item = await _context.BoardItems
            .FirstOrDefaultAsync(i => i.Id == request.BoardItemId, ct);
        if (item is null) throw new NotFoundException(nameof(BoardItem), request.BoardItemId);

        item.Archive(_currentUser.UserId, _timeProvider.UtcNow);
        return Result.Success();
    }
}
