using global::Notrelix.Application.Common.Models;

namespace Notrelix.Application.Features.Workspaces.Members.Commands.UpdateMemberRole;

public record UpdateMemberRoleCommand(
    Guid WorkspaceId,
    Guid UserId,
    WorkspaceRole Role
) : ICommand<Result>, ITransactionalRequest;

public class UpdateMemberRoleCommandHandler : IRequestHandler<UpdateMemberRoleCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IWorkspacePermissionService _permissions;

    public UpdateMemberRoleCommandHandler(
        IApplicationDbContext context,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider,
        IWorkspacePermissionService permissions)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
        _permissions = permissions;
    }

    public async Task<Result> Handle(UpdateMemberRoleCommand request, CancellationToken ct)
    {
        var workspace = await _context.Workspaces
            .FirstOrDefaultAsync(w => w.Id == request.WorkspaceId && w.Status == WorkspaceStatus.Active && !w.IsDeleted, ct);

        if (workspace is null)
            throw new NotFoundException(nameof(Workspace), request.WorkspaceId);

        await _permissions.EnsureCanManageWorkspaceAsync(request.WorkspaceId, _currentUser.UserId, ct);

        var member = await _context.WorkspaceMembers
            .FirstOrDefaultAsync(m => m.WorkspaceId == workspace.Id && m.UserId == request.UserId, ct);

        if (member is null)
            throw new NotFoundException("WorkspaceMember", request.UserId);

        var activeOwnerCount = await _context.WorkspaceMembers
            .CountAsync(m => m.WorkspaceId == workspace.Id && m.Role == WorkspaceRole.Owner && m.Status == WorkspaceMemberStatus.Active, ct);

        member.ChangeRole(request.Role, _currentUser.UserId, activeOwnerCount, _dateTimeProvider.UtcNow);
        return Result.Success();
    }
}
