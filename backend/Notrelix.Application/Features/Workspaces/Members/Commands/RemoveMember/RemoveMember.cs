using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Workspaces.DTOs;
using global::Notrelix.Domain.Identity;
using global::Notrelix.Domain.Workspaces;

namespace Notrelix.Application.Features.Workspaces.Members.Commands.RemoveMember;

public record RemoveMemberCommand(
    Guid WorkspaceId,
    Guid UserId
) : IRequest<Result>;

public class RemoveMemberCommandHandler : IRequestHandler<RemoveMemberCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IWorkspacePermissionService _permissions;

    public RemoveMemberCommandHandler(
        IApplicationDbContext context,
        ICurrentUser currentUser,
        IWorkspacePermissionService permissions)
    {
        _context = context;
        _currentUser = currentUser;
        _permissions = permissions;
    }

    public async Task<Result> Handle(RemoveMemberCommand request, CancellationToken ct)
    {
        var workspace = await _context.Workspaces
            .Include(w => w.Members)
            .FirstOrDefaultAsync(w => w.Id == request.WorkspaceId && !w.IsArchived, ct);

        if (workspace is null)
            throw new NotFoundException(nameof(Workspace), request.WorkspaceId);

        await _permissions.EnsureCanManageWorkspaceAsync(request.WorkspaceId, _currentUser.UserId, ct);

        workspace.RemoveMember(request.UserId, _currentUser.UserId);
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
