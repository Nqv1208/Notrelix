using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests;
using Notrelix.Application.Common.Requests.Security;
using Notrelix.Application.Features.Accounts.Abstractions;
using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Domain.Accounts.Accounts;
using Notrelix.Domain.Identity.Users;

namespace Notrelix.Application.Features.Workspaces.Invitations.Commands.AcceptInvitation;

public record AcceptInvitationResultDto(string WorkspaceSlug, Guid WorkspaceId);

public record AcceptInvitationCommand(string Token)
    : ICommand<Result<AcceptInvitationResultDto>>,
      IAuthenticatedRequest,
      ITokenScopedRequest,
      ITransactionalRequest
{
    TokenPurpose ITokenScopedRequest.TokenPurpose =>
        TokenPurpose.WorkspaceInvitation;

    UseCaseSecurityKind IUseCaseSecurityRequirement.SecurityKind =>
        UseCaseSecurityKind.TokenScoped;
}

public class AcceptInvitationCommandHandler : IRequestHandler<AcceptInvitationCommand, Result<AcceptInvitationResultDto>>
{
    private readonly IWorkspaceDbContext _workspaceContext;
    private readonly IIdentityUserLookupService _identityUserLookup;
    private readonly IAccountMembershipProvisioner _accountMembershipProvisioner;
    private readonly IAccountStatusReader _accountStatusReader;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AcceptInvitationCommandHandler(
        IWorkspaceDbContext workspaceContext,
        IIdentityUserLookupService identityUserLookup,
        IAccountMembershipProvisioner accountMembershipProvisioner,
        IAccountStatusReader accountStatusReader,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider)
    {
        _workspaceContext = workspaceContext;
        _identityUserLookup = identityUserLookup;
        _accountMembershipProvisioner = accountMembershipProvisioner;
        _accountStatusReader = accountStatusReader;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<AcceptInvitationResultDto>> Handle(
        AcceptInvitationCommand request, CancellationToken ct)
    {
        if (!_requestContext.IsAuthenticated || _requestContext.UserId == Guid.Empty)
            return Result<AcceptInvitationResultDto>.Failure(
                "You must be logged in to perform this action.");

        var currentUserId = _requestContext.UserId;

        var currentUser = await _identityUserLookup.FindByIdAsync(currentUserId, ct);

        if (currentUser is null)
            return Result<AcceptInvitationResultDto>.Failure(
                "Current user was not found.");

        if (currentUser.Status is not (UserStatus.Active or UserStatus.PendingVerification))
            return Result<AcceptInvitationResultDto>.Failure(
                "Your account must be active before accepting workspace invitations.");

        if (!currentUser.EmailConfirmed)
            return Result<AcceptInvitationResultDto>.Failure(
                "Email must be confirmed before accepting workspace invitations.");

        var tokenHash = InvitationTokenHash.Create(request.Token);
        var invitation = await _workspaceContext.WorkspaceInvitations
            .FirstOrDefaultAsync(i => i.Token == tokenHash, ct);

        if (invitation is null)
            throw new NotFoundException(nameof(WorkspaceInvitation), request.Token);

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

        var accountStatus = await _accountStatusReader.GetStatusAsync(
            workspace.AccountId, ct);

        if (accountStatus is null)
            return Result<AcceptInvitationResultDto>.Failure(
                "Account was not found.");

        if (accountStatus is not AccountStatus.Active and not AccountStatus.Trialing)
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

        await _accountMembershipProvisioner
            .EnsureWorkspaceInviteeAccountMembershipAsync(
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

        return Result<AcceptInvitationResultDto>.Success(
            new AcceptInvitationResultDto(workspace.Slug, invitation.WorkspaceId));
    }

    private static string NormalizeEmail(string email)
        => email.Trim().ToLowerInvariant();
}
