using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Accounts.Abstractions;
using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Domain.Accounts.Accounts;

namespace Notrelix.Application.Features.Workspaces.Invitations.Services;

/// <summary>
/// Result of accepting a workspace invitation, shared by the token-based and
/// by-id acceptance paths.
/// </summary>
public record AcceptInvitationResultDto(string WorkspaceSlug, Guid WorkspaceId);

/// <summary>
/// Single authoritative acceptance pipeline for workspace invitations.
///
/// Both acceptance commands (token-based and by-id) resolve the invitation to
/// an aggregate and then converge here, so every target email / workspace /
/// account / membership rule is evaluated exactly once per accepted path.
/// Rejected acceptance must be side-effect free: nothing is mutated and no
/// grant projection is written.
/// </summary>
public interface IInvitationAcceptanceService
{
    Task<Result<AcceptInvitationResultDto>> AcceptAsync(
        WorkspaceInvitation invitation,
        Guid actingUserId,
        CancellationToken cancellationToken);
}

public sealed class InvitationAcceptanceService : IInvitationAcceptanceService
{
    private readonly IWorkspaceDbContext _workspaceContext;
    private readonly IIdentityUserLookupService _identityUserLookup;
    private readonly IAccountMembershipProvisioner _accountMembershipProvisioner;
    private readonly IAccountStatusReader _accountStatusReader;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IAccessGrantProjectionService _grantProjection;

    public InvitationAcceptanceService(
        IWorkspaceDbContext workspaceContext,
        IIdentityUserLookupService identityUserLookup,
        IAccountMembershipProvisioner accountMembershipProvisioner,
        IAccountStatusReader accountStatusReader,
        IDateTimeProvider dateTimeProvider,
        IAccessGrantProjectionService grantProjection)
    {
        _workspaceContext = workspaceContext;
        _identityUserLookup = identityUserLookup;
        _accountMembershipProvisioner = accountMembershipProvisioner;
        _accountStatusReader = accountStatusReader;
        _dateTimeProvider = dateTimeProvider;
        _grantProjection = grantProjection;
    }

    public async Task<Result<AcceptInvitationResultDto>> AcceptAsync(
        WorkspaceInvitation invitation,
        Guid actingUserId,
        CancellationToken ct)
    {
        if (actingUserId == Guid.Empty)
            return Result<AcceptInvitationResultDto>.Failure(
                "You must be logged in to perform this action.");

        var currentUser = await _identityUserLookup.FindByIdAsync(actingUserId, ct);

        if (currentUser is null)
            return Result<AcceptInvitationResultDto>.Failure(
                "Current user was not found.");

        if (currentUser.Status is not (UserStatus.Active or UserStatus.PendingVerification))
            return Result<AcceptInvitationResultDto>.Failure(
                "Your account must be active before accepting workspace invitations.");

        if (!currentUser.EmailConfirmed)
            return Result<AcceptInvitationResultDto>.Failure(
                "Email must be confirmed before accepting workspace invitations.");

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

        var workspace = await _workspaceContext.Workspaces
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

        var existingMember = await _workspaceContext.WorkspaceMembers
            .FirstOrDefaultAsync(m =>
                m.WorkspaceId == invitation.WorkspaceId &&
                m.UserId == actingUserId, ct);

        if (existingMember is not null)
            return AcceptForExistingMember(
                invitation, existingMember.Status, actingUserId, workspace.Slug, now);

        await _accountMembershipProvisioner
            .EnsureWorkspaceInviteeAccountMembershipAsync(
                workspace.AccountId,
                actingUserId,
                invitation.InvitedBy,
                now,
                ct);

        invitation.Accept(actingUserId, now);

        var member = WorkspaceMember.Create(
            workspace.AccountId,
            invitation.WorkspaceId,
            actingUserId,
            invitation.Role,
            invitation.InvitedBy,
            now);

        _workspaceContext.WorkspaceMembers.Add(member);

        await _grantProjection.SyncWorkspaceMemberGrantAsync(
            workspace.AccountId,
            invitation.WorkspaceId,
            actingUserId,
            invitation.Role,
            now,
            ct);

        return Result<AcceptInvitationResultDto>.Success(
            new AcceptInvitationResultDto(workspace.Slug, invitation.WorkspaceId));
    }

    private static Result<AcceptInvitationResultDto> AcceptForExistingMember(
        WorkspaceInvitation invitation,
        WorkspaceMemberStatus status,
        Guid actingUserId,
        string workspaceSlug,
        DateTimeOffset now)
    {
        switch (status)
        {
            case WorkspaceMemberStatus.Active:
                // Idempotent consume: the user is already an active member, so no
                // duplicate membership, no grant projection and no role overwrite.
                invitation.Accept(actingUserId, now);
                return Result<AcceptInvitationResultDto>.Success(
                    new AcceptInvitationResultDto(workspaceSlug, invitation.WorkspaceId));

            case WorkspaceMemberStatus.Suspended:
                return Result<AcceptInvitationResultDto>.Failure(
                    "Your membership in this workspace is suspended. It must be reactivated before you can accept this invitation.");

            case WorkspaceMemberStatus.Removed:
                return Result<AcceptInvitationResultDto>.Failure(
                    "You were removed from this workspace and cannot accept this invitation. Contact a workspace administrator.");

            default:
                return Result<AcceptInvitationResultDto>.Failure(
                    "Your membership in this workspace is inactive.");
        }
    }

    private static string NormalizeEmail(string email)
        => email.Trim().ToLowerInvariant();
}