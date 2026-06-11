using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Workspaces.DTOs;
using global::Notrelix.Domain.Identity;
using global::Notrelix.Domain.Workspaces;

namespace Notrelix.Application.Features.Workspaces.Members.Commands.UpdateMemberRole;

public record UpdateMemberRoleCommand(
    Guid WorkspaceId,
    Guid UserId,
    string Role
) : IRequest<Result>;

public class UpdateMemberRoleCommandHandler : IRequestHandler<UpdateMemberRoleCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IWorkspacePermissionService _permissions;

    public UpdateMemberRoleCommandHandler(
        IApplicationDbContext context,
        ICurrentUser currentUser,
        IWorkspacePermissionService permissions)
    {
        _context = context;
        _currentUser = currentUser;
        _permissions = permissions;
    }

    public async Task<Result> Handle(UpdateMemberRoleCommand request, CancellationToken ct)
    {
        var workspace = await _context.Workspaces
            .Include(w => w.Members)
            .FirstOrDefaultAsync(w => w.Id == request.WorkspaceId && !w.IsArchived, ct);

        if (workspace is null)
            throw new NotFoundException(nameof(Workspace), request.WorkspaceId);

        await _permissions.EnsureCanManageWorkspaceAsync(request.WorkspaceId, _currentUser.UserId, ct);

        var newRole = Enum.Parse<WorkspaceRole>(request.Role, ignoreCase: true);
        workspace.ChangeMemberRole(request.UserId, newRole, _currentUser.UserId);
        await _context.SaveChangesAsync(ct);

        return Result.Success();
    }
}
