using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Models;

namespace Notrelix.Application.Features.Workspaces.Members.Commands.RemoveMember;

public record RemoveMemberCommand(
    Guid WorkspaceId,
    Guid UserId
) : ICommand<Result>, ITransactionalRequest;

public class RemoveMemberCommandHandler : IRequestHandler<RemoveMemberCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IWorkspacePermissionService _permissions;

    public RemoveMemberCommandHandler(
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

    public async Task<Result> Handle(RemoveMemberCommand request, CancellationToken ct)
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

        member.Remove(activeOwnerCount, _currentUser.UserId, _dateTimeProvider.UtcNow);
        return Result.Success();
    }
}
