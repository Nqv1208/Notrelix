using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.BoardViews.Commands.ArchiveBoardView;

[IdempotencyOperation("work-management.board-views.archive-board-view.v1")]
public record ArchiveBoardViewCommand(Guid BoardId, Guid ViewId, long ExpectedVersion, Guid? NewDefaultViewId = null) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest, IExpectedVersionRequest
{
    public PermissionAction Action => PermissionAction.UpdateBoardView;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board-view"), ViewId);
}

public class ArchiveBoardViewCommandHandler : IRequestHandler<ArchiveBoardViewCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IBoardRealtimeNotifier? _realtime;

    public ArchiveBoardViewCommandHandler(
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

    public async Task<Result> Handle(ArchiveBoardViewCommand request, CancellationToken ct)
    {
        var response = await HandleCore(request, ct);
        if (_realtime is not null) await _realtime.NotifyAsync(request, response, ct);
        return response;
    }

    private async Task<Result> HandleCore(ArchiveBoardViewCommand request, CancellationToken ct)
    {
        var view = await _context.BoardViews
            .FirstOrDefaultAsync(v => v.Id == request.ViewId && v.BoardId == request.BoardId, ct);
        if (view is null) throw new NotFoundException(nameof(BoardView), request.ViewId);

        BoardViewAccess.EnsureCanEdit(view, _currentUser.UserId);
        var now = _dateTimeProvider.UtcNow;
        if (!view.IsArchived)
            await BoardViewDefaults.HandOverAsync(_context, view, request.NewDefaultViewId, _currentUser.UserId, now, ct);
        view.Archive(_currentUser.UserId, now);
        return Result.Success();
    }
}
