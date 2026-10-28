using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.BoardViews.Commands.SetBoardViewVisibility;

/// <summary>
/// Shares a view with the Board's members or keeps it to its creator (WM-8). Only the creator changes who
/// sees a view, so another member's private view is not found and a Board view someone else made is refused.
/// </summary>
[IdempotencyOperation("work-management.board-views.set-board-view-visibility.v1")]
public record SetBoardViewVisibilityCommand(Guid BoardId, Guid ViewId, string Visibility)
    : ICommand<Result>, IWriteRequest, IRequirePermission, IAuthenticatedRequest, IResourceScopedRequest, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.UpdateBoardView;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board-view"), ViewId);
}

public class SetBoardViewVisibilityCommandHandler : IRequestHandler<SetBoardViewVisibilityCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IBoardRealtimeNotifier? _realtime;

    public SetBoardViewVisibilityCommandHandler(
        IWorkManagementDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider,
        IBoardRealtimeNotifier? realtime = null)
    {
        _realtime = realtime;
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(SetBoardViewVisibilityCommand request, CancellationToken ct)
    {
        var response = await HandleCore(request, ct);
        if (_realtime is not null) await _realtime.NotifyAsync(request, response, ct);
        return response;
    }

    private async Task<Result> HandleCore(SetBoardViewVisibilityCommand request, CancellationToken ct)
    {
        var view = await _context.BoardViews
            .FirstOrDefaultAsync(v => v.Id == request.ViewId && v.BoardId == request.BoardId, ct);
        if (view is null) throw new NotFoundException(nameof(BoardView), request.ViewId);

        var userId = _requestContext.UserId;
        BoardViewAccess.EnsureCanEdit(view, userId);
        if (view.CreatedBy != userId)
            return Result.Failure("Only the person who made a view changes who sees it.");

        if (!Enum.TryParse<BoardViewVisibility>(request.Visibility, true, out var visibility))
            return Result.Failure("The visibility is not one of Board or Private.");

        view.SetVisibility(visibility, userId, _dateTimeProvider.UtcNow);
        return Result.Success();
    }
}
