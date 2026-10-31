using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Features.Identity.Public.Queries;
using Notrelix.Application.Features.Workspaces.Invitations.Services;

namespace Notrelix.Application.Features.Workspaces.Invitations.Queries.GetUserPendingInvitations;

public record UserPendingInvitationDto(
    Guid Id,
    string WorkspaceName,
    string WorkspaceSlug,
    string InviterName,
    string Email,
    string Role,
    DateTime ExpiresAt
);

/// <summary>
/// The pending, unexpired invitations addressed to the signed-in person's own address, read through the
/// invitee policy of the database (RLS 019). An address that is not confirmed is not refused: it gets an empty
/// list (the handler and the policy both read nothing for it), so the rail never shows an error for it. The
/// address comes from the stored user, never from the token claim. At most
/// <see cref="InviteeInvitations.MaxPending"/> invitations come back, the newest first.
/// </summary>
public record GetUserPendingInvitationsQuery : IQuery<Result<List<UserPendingInvitationDto>>>, IAuthenticatedRequest, IReadRequest, IGlobalRequest;

public class GetUserPendingInvitationsQueryHandler : IRequestHandler<GetUserPendingInvitationsQuery, Result<List<UserPendingInvitationDto>>>
{
    private readonly InviteeInvitations _invitees;
    private readonly IIdentityUserFacts _identityUserFacts;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetUserPendingInvitationsQueryHandler(
        InviteeInvitations invitees,
        IIdentityUserFacts identityUserFacts,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider)
    {
        _invitees = invitees;
        _identityUserFacts = identityUserFacts;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<List<UserPendingInvitationDto>>> Handle(GetUserPendingInvitationsQuery request, CancellationToken ct)
    {
        if (!_requestContext.IsAuthenticated)
        {
            return Result<List<UserPendingInvitationDto>>.Success(new List<UserPendingInvitationDto>());
        }

        // The stored address, and only while it is confirmed: a claim in the token proves neither.
        var user = await _identityUserFacts.FindByIdAsync(_requestContext.UserId, ct);
        if (user is null || !user.CanParticipate || !user.EmailConfirmed)
        {
            return Result<List<UserPendingInvitationDto>>.Success(new List<UserPendingInvitationDto>());
        }

        var pending = await _invitees.ListPendingAsync(user.Email.Trim().ToLowerInvariant(), _dateTimeProvider.UtcNow, ct);

        var result = new List<UserPendingInvitationDto>();

        foreach (var p in pending)
        {
            var i = p.Invitation;
            // No name to show is an empty string: the client words that case in the reader's language.
            var inviterName = p.InviterName?.Trim() ?? "";

            result.Add(new UserPendingInvitationDto(
                i.Id,
                p.WorkspaceName,
                p.WorkspaceSlug,
                inviterName,
                i.Email,
                i.Role.ToString(),
                i.ExpiresAt.UtcDateTime
            ));
        }

        return Result<List<UserPendingInvitationDto>>.Success(result);
    }
}
