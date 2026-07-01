using global::Notrelix.Application.Common.Models;

namespace Notrelix.Application.Features.Workspaces.Invitations.Queries.GetUserPendingInvitations;

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

public record GetUserPendingInvitationsQuery : IQuery<Result<List<UserPendingInvitationDto>>>;

public class GetUserPendingInvitationsQueryHandler : IRequestHandler<GetUserPendingInvitationsQuery, Result<List<UserPendingInvitationDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetUserPendingInvitationsQueryHandler(IApplicationDbContext context, ICurrentUser currentUser, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<List<UserPendingInvitationDto>>> Handle(GetUserPendingInvitationsQuery request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(_currentUser.Email))
        {
            return Result<List<UserPendingInvitationDto>>.Success(new List<UserPendingInvitationDto>());
        }

        var userEmail = _currentUser.Email.Trim().ToLowerInvariant();
        var now = _dateTimeProvider.UtcNow;

        var invitations = await _context.WorkspaceInvitations
            .AsNoTracking()
            .Where(i => i.Email == userEmail && i.Status == WorkspaceInvitationStatus.Pending && i.ExpiresAt > now)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(ct);

        var result = new List<UserPendingInvitationDto>();

        foreach (var i in invitations)
        {
            var workspace = await _context.Workspaces.AsNoTracking()
                .FirstOrDefaultAsync(w => w.Id == i.WorkspaceId, ct);

            var inviter = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == i.InvitedBy, ct);

            var inviterName = inviter?.Name ?? "Ai đó";
            if (string.IsNullOrWhiteSpace(inviterName))
            {
                inviterName = inviter?.Email?.Value ?? "Người dùng Workspace";
            }

            result.Add(new UserPendingInvitationDto(
                i.Id,
                workspace?.Name ?? "Unknown",
                workspace?.Slug ?? "",
                inviterName,
                i.Email,
                i.Role.ToString(),
                i.Token.Value,
                i.ExpiresAt.DateTime
            ));
        }

        return Result<List<UserPendingInvitationDto>>.Success(result);
    }
}
