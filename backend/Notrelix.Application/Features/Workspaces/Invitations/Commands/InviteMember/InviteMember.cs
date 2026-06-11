using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Workspaces.DTOs;
using global::Notrelix.Domain.Identity;
using global::Notrelix.Domain.Workspaces;

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

    public InviteMemberCommandHandler(
        IApplicationDbContext context,
        ICurrentUser currentUser,
        IWorkspacePermissionService permissions)
    {
        _context = context;
        _currentUser = currentUser;
        _permissions = permissions;
    }

    public async Task<Result<Guid>> Handle(InviteMemberCommand request, CancellationToken ct)
    {
        var workspaceExists = await _context.Workspaces
            .AsNoTracking()
            .AnyAsync(w => w.Id == request.WorkspaceId && !w.IsArchived, ct);

        if (!workspaceExists)
            throw new NotFoundException(nameof(Workspace), request.WorkspaceId);

        await _permissions.EnsureCanManageWorkspaceAsync(request.WorkspaceId, _currentUser.UserId, ct);

        var cleanEmail = request.Email.Trim().ToLowerInvariant();

        // ── Kiểm tra email/user đã là thành viên chưa ─────────────
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

        // ── Kiểm tra lời mời active ─────────────────────────────
        var hasActiveInvitation = await _context.WorkspaceInvitations
            .AnyAsync(i => i.WorkspaceId == request.WorkspaceId 
                           && i.Email == cleanEmail
                           && i.AcceptedAt == null 
                           && i.ExpiresAt > DateTime.UtcNow, ct);

        if (hasActiveInvitation)
            return Result<Guid>.Failure("Đã có một lời mời đang chờ xử lý dành cho email này.");

        var role = Enum.Parse<WorkspaceRole>(request.Role, ignoreCase: true);
        var invitation = WorkspaceInvitation.Create(request.WorkspaceId, _currentUser.UserId, request.Email, role);

        _context.WorkspaceInvitations.Add(invitation);
        await _context.SaveChangesAsync(ct);

        return Result<Guid>.Success(invitation.Id);
    }
}
