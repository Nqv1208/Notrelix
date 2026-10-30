using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.BoardViews.Commands.DeleteBoardView;

[IdempotencyOperation("work-management.board-views.delete-board-view.v1")]
public record DeleteBoardViewCommand(Guid BoardId, Guid ViewId, long ExpectedVersion, Guid? NewDefaultViewId = null)
    : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest, IExpectedVersionRequest
{
    public PermissionAction Action => PermissionAction.UpdateBoardView;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board-view"), ViewId);
}

public class DeleteBoardViewCommandHandler : IRequestHandler<DeleteBoardViewCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DeleteBoardViewCommandHandler(
        IWorkManagementDbContext context,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(DeleteBoardViewCommand request, CancellationToken ct)
    {
        var view = await _context.BoardViews
            .FirstOrDefaultAsync(v => v.Id == request.ViewId && v.BoardId == request.BoardId, ct);

        if (view is null)
            throw new NotFoundException(nameof(BoardView), request.ViewId);

        BoardViewAccess.EnsureCanEdit(view, _currentUser.UserId);
        var now = _dateTimeProvider.UtcNow;
        await BoardViewDefaults.HandOverAsync(_context, view, request.NewDefaultViewId, _currentUser.UserId, now, ct);
        view.Delete(_currentUser.UserId, now);
        return Result.Success();
    }
}
