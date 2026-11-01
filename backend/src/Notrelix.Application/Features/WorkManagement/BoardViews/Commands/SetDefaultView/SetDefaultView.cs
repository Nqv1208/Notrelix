using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.BoardViews.Commands.SetDefaultView;

[IdempotencyOperation("work-management.board-views.set-default-view.v1")]
public record SetDefaultViewCommand(Guid BoardId, Guid ViewId, long ExpectedVersion)
    : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest, IExpectedVersionRequest
{
    // Choosing the default view is a view change (Editor), not Board management.
    public PermissionAction Action => PermissionAction.UpdateBoardView;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board-view"), ViewId);
}

public class SetDefaultViewCommandHandler : IRequestHandler<SetDefaultViewCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IBoardRealtimeNotifier? _realtime;

    public SetDefaultViewCommandHandler(
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

    public async Task<Result> Handle(SetDefaultViewCommand request, CancellationToken ct)
    {
        var response = await HandleCore(request, ct);
        if (_realtime is not null) await _realtime.NotifyAsync(request, response, ct);
        return response;
    }

    private async Task<Result> HandleCore(SetDefaultViewCommand request, CancellationToken ct)
    {
        var view = await _context.BoardViews
            .FirstOrDefaultAsync(v => v.Id == request.ViewId && v.BoardId == request.BoardId, ct);
        if (view is null) throw new NotFoundException(nameof(BoardView), request.ViewId);

        BoardViewAccess.EnsureCanEdit(view, _currentUser.UserId);
        var now = _dateTimeProvider.UtcNow;

        var otherDefaults = await _context.BoardViews
            .Where(v => v.BoardId == request.BoardId && v.Id != request.ViewId && v.IsDefault && v.DeletedAt == null)
            .ToListAsync(ct);

        foreach (var other in otherDefaults)
        {
            other.ClearDefault(_currentUser.UserId, now);
        }

        view.SetDefault(_currentUser.UserId, now);
        return Result.Success();
    }
}
