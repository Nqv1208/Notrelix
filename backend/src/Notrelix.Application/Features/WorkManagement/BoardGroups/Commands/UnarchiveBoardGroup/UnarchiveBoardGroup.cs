using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.BoardGroups.Commands.UnarchiveBoardGroup;

[IdempotencyOperation("work-management.board-groups.unarchive-board-group.v1")]
public record UnarchiveBoardGroupCommand(Guid GroupId, long ExpectedVersion) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest, IExpectedVersionRequest
{
    // Unarchiving a group edits Board content (Editor), not Board management.
    public PermissionAction Action => PermissionAction.UpdateItem;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board-group"), GroupId);
}

public class UnarchiveBoardGroupCommandHandler : IRequestHandler<UnarchiveBoardGroupCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IBoardRealtimeNotifier? _realtime;

    public UnarchiveBoardGroupCommandHandler(
        IWorkManagementDbContext context,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider,
        IBoardRealtimeNotifier? realtime = null)
    {
        _realtime = realtime;
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(UnarchiveBoardGroupCommand request, CancellationToken ct)
    {
        var response = await HandleCore(request, ct);
        if (_realtime is not null) await _realtime.NotifyAsync(request, response, ct);
        return response;
    }

    private async Task<Result> HandleCore(UnarchiveBoardGroupCommand request, CancellationToken ct)
    {
        var group = await _context.BoardGroups.FirstOrDefaultAsync(g => g.Id == request.GroupId, ct);
        if (group is null) throw new NotFoundException(nameof(BoardGroup), request.GroupId);
        group.Unarchive(_currentUser.UserId, _dateTimeProvider.UtcNow);
        return Result.Success();
    }
}
