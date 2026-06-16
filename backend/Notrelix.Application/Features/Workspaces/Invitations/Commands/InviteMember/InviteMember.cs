using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Domain.Workspaces.Invitations;
using global::Notrelix.Domain.SharedKernel;
using global::Notrelix.Domain.Workspaces.Invitations;
using global::Notrelix.Domain.Workspaces.Members;
using global::Notrelix.Domain.Workspaces.Workspaces;

namespace Notrelix.Application.Features.Workspaces.Invitations.Commands.InviteMember;

public record InviteMemberCommand(
    Guid WorkspaceId,
    string Email,
    string Role
) : IRequest<Result<Guid>>;

public class InviteMemberCommandHandler : IRequestHandler<InviteMemberCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IWorkspacePermissionService _permissions;
    private readonly IDateTimeProvider _dateTimeProvider;

    public InviteMemberCommandHandler(
        IApplicationDbContext context,
        ICurrentUser currentUser,
        IWorkspacePermissionService permissions,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _permissions = permissions;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(InviteMemberCommand request, CancellationToken ct)
    {
        var workspaceExists = await _context.Workspaces
            .AsNoTracking()
            .AnyAsync(w => w.Id == request.WorkspaceId && w.Status == WorkspaceStatus.Active && !w.IsDeleted, ct);

        if (!workspaceExists)
            throw new NotFoundException(nameof(Workspace), request.WorkspaceId);

        await _permissions.EnsureCanManageWorkspaceAsync(request.WorkspaceId, _currentUser.UserId, ct);

        var cleanEmail = request.Email.Trim().ToLowerInvariant();
        var now = _dateTimeProvider.UtcNow;

        var targetUser = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email.Value.ToLower() == cleanEmail, ct);

        if (targetUser != null)
        {
            var isAlreadyMember = await _context.WorkspaceMembers
                .AnyAsync(m => m.WorkspaceId == request.WorkspaceId && m.UserId == targetUser.Id, ct);

            if (isAlreadyMember)
                return Result<Guid>.Failure("Người dùng này đã là thành viên của Workspace.");
        }

        var hasActiveInvitation = await _context.WorkspaceInvitations
            .AnyAsync(i => i.WorkspaceId == request.WorkspaceId 
                           && i.Email == cleanEmail
                           && i.Status == WorkspaceInvitationStatus.Pending
                           && i.ExpiresAt > now, ct);

        if (hasActiveInvitation)
            return Result<Guid>.Failure("Đã có một lời mời đang chờ xử lý dành cho email này.");

        var role = Enum.Parse<WorkspaceRole>(request.Role, ignoreCase: true);
        var token = InvitationTokenHash.Create(Guid.NewGuid().ToString("N"));
        var invitation = WorkspaceInvitation.Create(request.WorkspaceId, cleanEmail, role, token, _currentUser.UserId, now);

        _context.WorkspaceInvitations.Add(invitation);
        await _context.SaveChangesAsync(ct);

        return Result<Guid>.Success(invitation.Id);
    }
}
