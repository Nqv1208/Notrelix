using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.BoardGroups.Commands.UnarchiveBoardGroup;

[IdempotencyOperation("work-management.board-groups.unarchive-board-group.v1")]
public record UnarchiveBoardGroupCommand(Guid GroupId) : ICommand<Result>, ITransactionalRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.ManageBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.BoardGroup, GroupId);
}

public class UnarchiveBoardGroupCommandHandler : IRequestHandler<UnarchiveBoardGroupCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UnarchiveBoardGroupCommandHandler(
        IWorkManagementDbContext context,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(UnarchiveBoardGroupCommand request, CancellationToken ct)
    {
        var group = await _context.BoardGroups.FirstOrDefaultAsync(g => g.Id == request.GroupId, ct);
        if (group is null) throw new NotFoundException(nameof(BoardGroup), request.GroupId);
        group.Unarchive(_currentUser.UserId, _dateTimeProvider.UtcNow);
        return Result.Success();
    }
}
