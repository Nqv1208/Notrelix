using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Features.Identity.Public.Queries;
using Notrelix.Application.Features.Workspaces.Invitations.Services;

namespace Notrelix.Application.Features.Workspaces.Invitations.Commands.DeclineInvitation;

/// <summary>
/// The invited person declines a pending invitation they see in their own list. The invitation must be
/// addressed to their confirmed address and the person must be able to take part (as when listing and
/// accepting): another address, an unconfirmed address, an account that cannot take part, a repeat and an
/// unknown id all answer the same NotFound. An invitation that expired meanwhile cannot be declined.
/// </summary>
public record DeclineInvitationCommand(
    Guid InvitationId
) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IGlobalRequest
{
}

public class DeclineInvitationCommandHandler : IRequestHandler<DeclineInvitationCommand, Result>
{
    private readonly InviteeInvitations _invitees;
    private readonly IIdentityUserFacts _identityUserFacts;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DeclineInvitationCommandHandler(
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

    public async Task<Result> Handle(DeclineInvitationCommand request, CancellationToken ct)
    {
        var userId = _requestContext.UserId;
        var user = await _identityUserFacts.FindByIdAsync(userId, ct);

        if (user is not { EmailConfirmed: true, CanParticipate: true })
            throw new NotFoundException(nameof(WorkspaceInvitation), request.InvitationId);

        var invitation = await _invitees.OpenAsync(
            request.InvitationId, userId, user.Email.Trim().ToLowerInvariant(), ct);

        if (invitation is null)
            throw new NotFoundException(nameof(WorkspaceInvitation), request.InvitationId);

        var now = _dateTimeProvider.UtcNow;
        if (now >= invitation.ExpiresAt)
            return Result.Failure("This invitation has expired.");

        invitation.Decline(userId, now);
        return Result.Success();
    }
}
