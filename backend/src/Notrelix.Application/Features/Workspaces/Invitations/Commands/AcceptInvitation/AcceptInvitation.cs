using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Tokens;
using Notrelix.Application.Features.Accounts.Public.Membership;
using Notrelix.Application.Features.Governance.Public.Access;
using Notrelix.Application.Features.Identity.Public.Queries;
using Notrelix.Application.Features.Workspaces.Invitations.Services;
using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Application.Features.Workspaces.Members.Services;

namespace Notrelix.Application.Features.Workspaces.Invitations.Commands.AcceptInvitation;

/// <summary>
/// Where the accepted invitation leads. <see cref="ResourceKind"/> and <see cref="ResourceId"/> name the
/// one Board or Page of a resource invitation; both are null for a plain Workspace invitation.
/// </summary>
public record AcceptInvitationResultDto(
    string WorkspaceSlug,
    Guid WorkspaceId,
    string? ResourceKind = null,
    Guid? ResourceId = null);

public record AcceptInvitationCommand(string Token)
    : ICommand<Result<AcceptInvitationResultDto>>,
      IAuthenticatedTokenScopedRequest,
      IRequireVerifiedEmail,
      IWriteRequest
{
    TokenPurpose ITokenScopedRequest.TokenPurpose =>
        TokenPurpose.WorkspaceInvitation;

}

public class AcceptInvitationCommandHandler : IRequestHandler<AcceptInvitationCommand, Result<AcceptInvitationResultDto>>
{
    private readonly IWorkspaceDbContext _workspaceContext;
    private readonly IIdentityUserFacts _identityUserFacts;
    private readonly IOneTimeTokenService _oneTimeTokenService;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly InvitationAcceptanceService _acceptance;
    private readonly InvitationTokenScope _tokenScope;

    public AcceptInvitationCommandHandler(
        IWorkspaceDbContext workspaceContext,
        IIdentityUserFacts identityUserFacts,
        IAccountMembershipActions accountMembershipActions,
        IAccountMembershipFacts accountMembershipFacts,
        IOneTimeTokenService oneTimeTokenService,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider,
        IWorkspaceGrantProjectionService grantProjection,
        InvitationInitialEntries initialEntries,
        IGuestResourceAccessActions guestResourceAccess,
        IAuditService auditService,
        InvitationTokenScope tokenScope,
        Notrelix.Application.Features.Billing.Public.Seats.IBillingSeatGate? seatGate = null)
    {
        _tokenScope = tokenScope;
        _workspaceContext = workspaceContext;
        _identityUserFacts = identityUserFacts;
        _oneTimeTokenService = oneTimeTokenService;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
        _acceptance = new InvitationAcceptanceService(
            workspaceContext,
            accountMembershipActions,
            accountMembershipFacts,
            grantProjection,
            initialEntries,
            guestResourceAccess,
            auditService,
            seatGate);
    }

    public async Task<Result<AcceptInvitationResultDto>> Handle(
        AcceptInvitationCommand request, CancellationToken ct)
    {
        if (!_requestContext.IsAuthenticated || _requestContext.UserId == Guid.Empty)
            return Result<AcceptInvitationResultDto>.Failure(
                "You must be logged in to perform this action.");

        var currentUserId = _requestContext.UserId;

        var currentUser = await _identityUserFacts.FindByIdAsync(currentUserId, ct);

        if (currentUser is null)
            return Result<AcceptInvitationResultDto>.Failure(
                "Current user was not found.");

        if (!currentUser.CanParticipate)
            return Result<AcceptInvitationResultDto>.Failure(
                "Your account must be active before accepting workspace invitations.");

        if (!currentUser.EmailConfirmed)
            return Result<AcceptInvitationResultDto>.Failure(
                "Email must be confirmed before accepting workspace invitations.");

        ParsedOneTimeToken presentedHash;
        try
        {
            presentedHash = _oneTimeTokenService.ParseAndHash(
                request.Token,
                TokenPurpose.WorkspaceInvitation);
        }
        catch (InvalidOneTimeTokenException)
        {
            return Result<AcceptInvitationResultDto>.Failure(
                "Invalid or expired invitation token.");
        }

        var invitation = await _tokenScope.FindAsync(presentedHash, track: true, ct);

        if (invitation is null)
            throw new NotFoundException(nameof(WorkspaceInvitation), "Invalid invitation token.");

        await _tokenScope.AdoptAsync(invitation, currentUserId, ct);

        var now = _dateTimeProvider.UtcNow;

        return await _acceptance.AcceptAsync(invitation, currentUserId, currentUser.Email, now, ct);
    }
}
