using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Features.Identity.Public.Queries;
using Notrelix.Application.Features.Workspaces.Invitations.Commands.AcceptInvitation;
using Notrelix.Application.Features.Workspaces.Invitations.Services;

namespace Notrelix.Application.Features.Workspaces.Invitations.Commands.AcceptInvitationById;

/// <summary>
/// The invited person accepts a pending invitation they see in their own list. It follows the rules of
/// accepting by link (<see cref="InvitationAcceptanceService"/>: address, seat, Suspended and Removed
/// members, entries, resource entry) once the invitation is opened for them: an invitation that is not
/// pending, not addressed to their confirmed address or unknown answers the same NotFound.
/// </summary>
public record AcceptInvitationByIdCommand(Guid InvitationId)
    : ICommand<Result<AcceptInvitationResultDto>>,
      IAuthenticatedRequest,
      IGlobalRequest,
      IRequireVerifiedEmail,
      IWriteRequest;

public class AcceptInvitationByIdCommandHandler : IRequestHandler<AcceptInvitationByIdCommand, Result<AcceptInvitationResultDto>>
{
    private readonly InviteeInvitations _invitees;
    private readonly InvitationAcceptanceService _acceptance;
    private readonly IIdentityUserFacts _identityUserFacts;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AcceptInvitationByIdCommandHandler(
        InviteeInvitations invitees,
        InvitationAcceptanceService acceptance,
        IIdentityUserFacts identityUserFacts,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider)
    {
        _invitees = invitees;
        _acceptance = acceptance;
        _identityUserFacts = identityUserFacts;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<AcceptInvitationResultDto>> Handle(
        AcceptInvitationByIdCommand request, CancellationToken ct)
    {
        if (!_requestContext.IsAuthenticated || _requestContext.UserId == Guid.Empty)
            return Result<AcceptInvitationResultDto>.Failure(
                "You must be logged in to perform this action.");

        var userId = _requestContext.UserId;
        var currentUser = await _identityUserFacts.FindByIdAsync(userId, ct);

        if (currentUser is null)
            return Result<AcceptInvitationResultDto>.Failure(
                "Current user was not found.");

        if (!currentUser.CanParticipate)
            return Result<AcceptInvitationResultDto>.Failure(
                "Your account must be active before accepting workspace invitations.");

        if (!currentUser.EmailConfirmed)
            return Result<AcceptInvitationResultDto>.Failure(
                "Email must be confirmed before accepting workspace invitations.");

        var invitation = await _invitees.OpenAsync(
            request.InvitationId, userId, currentUser.Email.Trim().ToLowerInvariant(), ct);

        if (invitation is null)
            throw new NotFoundException(nameof(WorkspaceInvitation), request.InvitationId);

        return await _acceptance.AcceptAsync(
            invitation, userId, currentUser.Email, _dateTimeProvider.UtcNow, ct);
    }
}

public class AcceptInvitationByIdCommandValidator : AbstractValidator<AcceptInvitationByIdCommand>
{
    public AcceptInvitationByIdCommandValidator()
    {
        RuleFor(x => x.InvitationId).NotEmpty();
    }
}
