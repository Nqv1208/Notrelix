using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.BoardViews.Commands.SetDefaultView;

[IdempotencyOperation("work-management.board-views.set-default-view.v1")]
public record SetDefaultViewCommand(Guid BoardId, Guid ViewId)
    : ICommand<Result>, ITransactionalRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.ManageBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.Board, BoardId);
}

public class SetDefaultViewCommandHandler : IRequestHandler<SetDefaultViewCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public SetDefaultViewCommandHandler(
        IWorkManagementDbContext context,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(SetDefaultViewCommand request, CancellationToken ct)
    {
        var view = await _context.BoardViews
            .FirstOrDefaultAsync(v => v.Id == request.ViewId && v.BoardId == request.BoardId, ct);
        if (view is null) throw new NotFoundException(nameof(BoardView), request.ViewId);

        var now = _dateTimeProvider.UtcNow;

        var otherDefaults = await _context.BoardViews
            .Where(v => v.BoardId == request.BoardId && v.Id != request.ViewId && v.IsDefault && !v.IsDeleted)
            .ToListAsync(ct);

        foreach (var other in otherDefaults)
        {
            other.ClearDefault(_currentUser.UserId, now);
        }

        view.SetDefault(_currentUser.UserId, now);
        return Result.Success();
    }
}
