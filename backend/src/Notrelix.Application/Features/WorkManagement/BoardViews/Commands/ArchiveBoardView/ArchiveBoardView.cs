using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.BoardViews.Commands.ArchiveBoardView;

[IdempotencyOperation("work-management.board-views.archive-board-view.v1")]
public record ArchiveBoardViewCommand(Guid BoardId, Guid ViewId) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.UpdateBoardView;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board-view"), ViewId);
}

public class ArchiveBoardViewCommandHandler : IRequestHandler<ArchiveBoardViewCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ArchiveBoardViewCommandHandler(
        IWorkManagementDbContext context,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(ArchiveBoardViewCommand request, CancellationToken ct)
    {
        var view = await _context.BoardViews
            .FirstOrDefaultAsync(v => v.Id == request.ViewId && v.BoardId == request.BoardId, ct);
        if (view is null) throw new NotFoundException(nameof(BoardView), request.ViewId);

        view.Archive(_currentUser.UserId, _dateTimeProvider.UtcNow);
        return Result.Success();
    }
}
