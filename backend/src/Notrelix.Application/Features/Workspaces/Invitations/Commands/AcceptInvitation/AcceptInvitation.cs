using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Tokens;
using Notrelix.Application.Features.Accounts.Public.Commands;
using Notrelix.Application.Features.Accounts.Public.Queries;
using Notrelix.Application.Features.Identity.Public.Queries;
using Notrelix.Application.Features.Workspaces.Abstractions;

namespace Notrelix.Application.Features.Workspaces.Invitations.Commands.AcceptInvitation;

public record AcceptInvitationResultDto(string WorkspaceSlug, Guid WorkspaceId);

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
    private readonly IAccountMembershipActions _accountMembershipActions;
    private readonly IAccountMembershipFacts _accountMembershipFacts;
    private readonly IOneTimeTokenService _oneTimeTokenService;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IAccessGrantProjectionService _grantProjection;

    public AcceptInvitationCommandHandler(
        IWorkspaceDbContext workspaceContext,
        IIdentityUserFacts identityUserFacts,
        IAccountMembershipActions accountMembershipActions,
        IAccountMembershipFacts accountMembershipFacts,
        IOneTimeTokenService oneTimeTokenService,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider,
        IAccessGrantProjectionService grantProjection)
    {
        _workspaceContext = workspaceContext;
        _identityUserFacts = identityUserFacts;
        _accountMembershipActions = accountMembershipActions;
        _accountMembershipFacts = accountMembershipFacts;
        _oneTimeTokenService = oneTimeTokenService;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
        _grantProjection = grantProjection;
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

        var invitation = await _workspaceContext.WorkspaceInvitations
            .FirstOrDefaultAsync(
                i => i.Token.Value == presentedHash.TokenHash
                    && i.HashVersion == presentedHash.HashVersion, ct);

        if (invitation is null)
            throw new NotFoundException(nameof(WorkspaceInvitation), "Invalid invitation token.");

        var now = _dateTimeProvider.UtcNow;

        if (now >= invitation.ExpiresAt)
            return Result<AcceptInvitationResultDto>.Failure(
                "This invitation has expired.");

        if (invitation.Status != WorkspaceInvitationStatus.Pending)
            return Result<AcceptInvitationResultDto>.Failure(
                "This invitation is no longer valid.");

        var currentEmail = NormalizeEmail(currentUser.Email);
        var invitedEmail = NormalizeEmail(invitation.Email);

        if (currentEmail != invitedEmail)
            return Result<AcceptInvitationResultDto>.Failure(
                "This invitation belongs to a different email address.");

        var workspace = await _workspaceContext.Workspaces.AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == invitation.WorkspaceId, ct);

        if (workspace is null)
            throw new NotFoundException(nameof(Workspace), invitation.WorkspaceId);

        if (workspace.Status != WorkspaceStatus.Active)
            return Result<AcceptInvitationResultDto>.Failure(
                "Cannot accept invitation for an inactive workspace.");

        var accountAdmission = await _accountMembershipFacts.GetAdmissionAsync(
            workspace.AccountId, ct);

        if (accountAdmission is null)
            return Result<AcceptInvitationResultDto>.Failure(
                "Account was not found.");

        if (!accountAdmission.CanAdmitMember)
            return Result<AcceptInvitationResultDto>.Failure(
                "Cannot accept invitation for an inactive account.");

        var isAlreadyMember = await _workspaceContext.WorkspaceMembers
            .AnyAsync(m =>
                m.WorkspaceId == invitation.WorkspaceId &&
                m.UserId == currentUserId, ct);

        if (isAlreadyMember)
        {
            invitation.Accept(currentUserId, now);
            return Result<AcceptInvitationResultDto>.Success(
                new AcceptInvitationResultDto(workspace.Slug, invitation.WorkspaceId));
        }

        await _accountMembershipActions
            .EnsureWorkspaceInviteeMembershipAsync(
                workspace.AccountId,
                currentUserId,
                invitation.InvitedBy,
                now,
                ct);

        invitation.Accept(currentUserId, now);

        var member = WorkspaceMember.Create(
            workspace.AccountId,
            invitation.WorkspaceId,
            currentUserId,
            invitation.Role,
            invitation.InvitedBy,
            now);

        _workspaceContext.WorkspaceMembers.Add(member);

        await _grantProjection.SyncWorkspaceMemberGrantAsync(
            workspace.AccountId,
            invitation.WorkspaceId,
            currentUserId,
            invitation.Role,
            now,
            ct);

        return Result<AcceptInvitationResultDto>.Success(
            new AcceptInvitationResultDto(workspace.Slug, invitation.WorkspaceId));
    }

    private static string NormalizeEmail(string email)
        => email.Trim().ToLowerInvariant();
}
