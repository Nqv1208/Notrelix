using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Common.Idempotency;

namespace Notrelix.Application.Features.WorkManagement.BoardGroups.Commands.ArchiveBoardGroup;

[IdempotencyOperation("work-management.board-groups.archive-board-group.v1")]
public record ArchiveBoardGroupCommand(Guid GroupId, string? IdempotencyKey = null) : ICommand<Result>, ITransactionalRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.ManageBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.BoardGroup, GroupId);
    string IIdempotentRequest.IdempotencyKey => IdempotencyKey ?? $"archive-group:{GroupId}";
}

public class ArchiveBoardGroupCommandHandler : IRequestHandler<ArchiveBoardGroupCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ArchiveBoardGroupCommandHandler(
        IWorkManagementDbContext context,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(ArchiveBoardGroupCommand request, CancellationToken ct)
    {
        var group = await _context.BoardGroups.FirstOrDefaultAsync(g => g.Id == request.GroupId, ct);
        if (group is null) throw new NotFoundException(nameof(BoardGroup), request.GroupId);
        group.Archive(_currentUser.UserId, _dateTimeProvider.UtcNow);
        return Result.Success();
    }
}
