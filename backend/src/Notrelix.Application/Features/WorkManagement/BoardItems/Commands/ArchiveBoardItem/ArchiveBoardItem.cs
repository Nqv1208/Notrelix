using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.ArchiveBoardItem;

[IdempotencyOperation("work-management.board-items.archive-board-item.v1")]
public record ArchiveBoardItemCommand(Guid BoardItemId, long ExpectedVersion) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest, IExpectedVersionRequest
{
    public PermissionAction Action => PermissionAction.UpdateItem;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board-item"), BoardItemId);
}

public class ArchiveBoardItemCommandHandler : IRequestHandler<ArchiveBoardItemCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _timeProvider;
    private readonly IBoardRealtimeNotifier? _realtime;

    public ArchiveBoardItemCommandHandler(
        IWorkManagementDbContext context,
        ICurrentUser currentUser,
        IDateTimeProvider timeProvider,
        IBoardRealtimeNotifier? realtime = null)
    {
        _realtime = realtime;
        _context = context;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result> Handle(ArchiveBoardItemCommand request, CancellationToken ct)
    {
        var response = await HandleCore(request, ct);
        if (_realtime is not null) await _realtime.NotifyAsync(request, response, ct);
        return response;
    }

    private async Task<Result> HandleCore(ArchiveBoardItemCommand request, CancellationToken ct)
    {
        var item = await _context.BoardItems
            .FirstOrDefaultAsync(i => i.Id == request.BoardItemId, ct);
        if (item is null) throw new NotFoundException(nameof(BoardItem), request.BoardItemId);

        var now = _timeProvider.UtcNow;
        item.Archive(_currentUser.UserId, now);

        // A parent takes its sub-items with it; the ones already archived on their own stay as they are.
        var children = await _context.BoardItems
            .Where(child => child.ParentItemId == item.Id && child.DeletedAt == null && !child.IsArchived)
            .ToListAsync(ct);
        foreach (var child in children) child.ArchiveWithParent(_currentUser.UserId, now);
        return Result.Success();
    }
}
