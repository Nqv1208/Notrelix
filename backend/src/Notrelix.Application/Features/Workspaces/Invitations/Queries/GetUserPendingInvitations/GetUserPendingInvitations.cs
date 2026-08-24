using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Features.Workspaces.Abstractions;

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

public record GetUserPendingInvitationsQuery : IQuery<Result<List<UserPendingInvitationDto>>>, IAuthenticatedRequest, IReadRequest, IGlobalRequest;

public class GetUserPendingInvitationsQueryHandler : IRequestHandler<GetUserPendingInvitationsQuery, Result<List<UserPendingInvitationDto>>>
{
    private readonly IWorkspaceDbContext _context;
    private readonly IActorLookupService _actorLookup;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetUserPendingInvitationsQueryHandler(IWorkspaceDbContext context, IActorLookupService actorLookup, ICurrentRequestContext requestContext, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _actorLookup = actorLookup;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<List<UserPendingInvitationDto>>> Handle(GetUserPendingInvitationsQuery request, CancellationToken ct)
    {
        if (!_requestContext.IsAuthenticated)
        {
            return Result<List<UserPendingInvitationDto>>.Success(new List<UserPendingInvitationDto>());
        }

        var userEmail = _requestContext.Email.Trim().ToLowerInvariant();
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

            var inviter = await _actorLookup.FindAsync(i.InvitedBy, ct);
            var inviterName = inviter?.Name ?? "Ai đó";
            if (string.IsNullOrWhiteSpace(inviterName))
            {
                inviterName = "Người dùng Workspace";
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
