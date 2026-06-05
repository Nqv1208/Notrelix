using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Interfaces;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Domain.Entities.Workspaces;

namespace Notrelix.Application.Features.Workspaces.Queries.GetUserPendingInvitations;

public record UserPendingInvitationDto(
    Guid Id,
    string WorkspaceName,
    string WorkspaceSlug,
    string InviterName,
    string Email,
    string Role,
    string Token,
    DateTime ExpiresAt
);

public record GetUserPendingInvitationsQuery : IRequest<Result<List<UserPendingInvitationDto>>>;

public class GetUserPendingInvitationsQueryHandler : IRequestHandler<GetUserPendingInvitationsQuery, Result<List<UserPendingInvitationDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;

    public GetUserPendingInvitationsQueryHandler(IApplicationDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<List<UserPendingInvitationDto>>> Handle(GetUserPendingInvitationsQuery request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(_currentUser.Email))
        {
            return Result<List<UserPendingInvitationDto>>.Success(new List<UserPendingInvitationDto>());
        }

        var userEmail = _currentUser.Email.Trim().ToLowerInvariant();

        var invitations = await _context.WorkspaceInvitations
            .Include(i => i.Workspace)
            .AsNoTracking()
            .Where(i => i.Email == userEmail && i.AcceptedAt == null && i.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(ct);

        var result = new List<UserPendingInvitationDto>();

        foreach (var i in invitations)
        {
            var inviter = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == i.InvitedBy, ct);
            
            var inviterName = inviter != null ? inviter.Name : "Ai đó";
            if (string.IsNullOrWhiteSpace(inviterName))
            {
                inviterName = inviter?.Email?.Value ?? "Người dùng Workspace";
            }

            result.Add(new UserPendingInvitationDto(
                i.Id,
                i.Workspace.Name,
                i.Workspace.Slug,
                inviterName,
                i.Email,
                i.Role.ToString(),
                i.Token,
                i.ExpiresAt
            ));
        }

        return Result<List<UserPendingInvitationDto>>.Success(result);
    }
}
