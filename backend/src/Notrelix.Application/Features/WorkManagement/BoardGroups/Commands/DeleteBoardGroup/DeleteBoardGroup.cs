using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.BoardGroups.Commands.DeleteBoardGroup;

[IdempotencyOperation("work-management.board-groups.delete-board-group.v1")]
public record DeleteBoardGroupCommand(Guid GroupId)
    : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest, IExpectedVersionRequest
{
    public PermissionAction Action => PermissionAction.ManageBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board-group"), GroupId);
    long IExpectedVersionRequest.ExpectedVersion => 0;
}

public class DeleteBoardGroupCommandHandler : IRequestHandler<DeleteBoardGroupCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DeleteBoardGroupCommandHandler(
        IWorkManagementDbContext context,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(DeleteBoardGroupCommand request, CancellationToken ct)
    {
        var group = await _context.BoardGroups
            .FirstOrDefaultAsync(g => g.Id == request.GroupId, ct);
        if (group is null) throw new NotFoundException(nameof(BoardGroup), request.GroupId);

        var now = _dateTimeProvider.UtcNow;
        group.Delete(_currentUser.UserId, now);
        return Result.Success();
    }
}
