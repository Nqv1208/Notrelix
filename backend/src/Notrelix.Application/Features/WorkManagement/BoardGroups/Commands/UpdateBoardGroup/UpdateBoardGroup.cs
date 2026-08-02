using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.BoardGroups.Commands.UpdateBoardGroup;

[IdempotencyOperation("work-management.board-groups.update-board-group.v1")]
public record UpdateBoardGroupCommand(Guid GroupId, string? Title, string? Color = null, string? IdempotencyKey = null) : ICommand<Result>, ITransactionalRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.ManageBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.BoardGroup, GroupId);
    string IIdempotentRequest.IdempotencyKey => IdempotencyKey ?? $"update-group:{GroupId}";
}

public class UpdateBoardGroupCommandHandler : IRequestHandler<UpdateBoardGroupCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateBoardGroupCommandHandler(
        IWorkManagementDbContext context,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(UpdateBoardGroupCommand request, CancellationToken ct)
    {
        var list = await _context.BoardGroups.FirstOrDefaultAsync(l => l.Id == request.GroupId, ct);
        if (list is null) throw new NotFoundException(nameof(BoardGroup), request.GroupId);

        var now = _dateTimeProvider.UtcNow;
        if (request.Title is not null) list.Rename(request.Title, _currentUser.UserId, now);
        if (request.Color is not null) list.UpdateColor(Color.Create(request.Color), _currentUser.UserId, now);
        return Result.Success();
    }
}
