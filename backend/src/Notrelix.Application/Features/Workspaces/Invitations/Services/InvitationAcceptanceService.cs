using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Accounts.Public.Membership;
using Notrelix.Application.Features.Governance.Public.Access;
using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Application.Features.Workspaces.Invitations.Commands.AcceptInvitation;
using Notrelix.Application.Features.Workspaces.Members.Services;

namespace Notrelix.Application.Features.Workspaces.Invitations.Services;

/// <summary>
/// The acceptance rules of a Workspace invitation for an already identified user (WSP-009, WSP-026):
/// every check that can reject runs before the first mutation, the membership, the guest resource
/// entry and the initial Team/Space entries are applied together, and a repeat acceptance by the
/// same user is a safe no-op. Callers own the request transaction and the identity of the user.
/// </summary>
public sealed class InvitationAcceptanceService
{
    private readonly IWorkspaceDbContext _workspaceContext;
    private readonly IAccountMembershipActions _accountMembershipActions;
    private readonly IAccountMembershipFacts _accountMembershipFacts;
    private readonly IWorkspaceGrantProjectionService _grantProjection;
    private readonly InvitationInitialEntries _initialEntries;
    private readonly IGuestResourceAccessActions _guestResourceAccess;
    private readonly IAuditService _auditService;

    public InvitationAcceptanceService(
        IWorkspaceDbContext workspaceContext,
        IAccountMembershipActions accountMembershipActions,
        IAccountMembershipFacts accountMembershipFacts,
        IWorkspaceGrantProjectionService grantProjection,
        InvitationInitialEntries initialEntries,
        IGuestResourceAccessActions guestResourceAccess,
        IAuditService auditService)
    {
        _workspaceContext = workspaceContext;
        _accountMembershipActions = accountMembershipActions;
        _accountMembershipFacts = accountMembershipFacts;
        _grantProjection = grantProjection;
        _initialEntries = initialEntries;
        _guestResourceAccess = guestResourceAccess;
        _auditService = auditService;
    }

    public async Task<Result<AcceptInvitationResultDto>> AcceptAsync(
        WorkspaceInvitation invitation,
        Guid userId,
        string userEmail,
        DateTimeOffset now,
        CancellationToken ct)
    {
        if (now >= invitation.ExpiresAt)
            return Result<AcceptInvitationResultDto>.Failure(
                "This invitation has expired.");

        if (invitation.Status != WorkspaceInvitationStatus.Pending)
            return Result<AcceptInvitationResultDto>.Failure(
                "This invitation is no longer valid.");

        var currentEmail = NormalizeEmail(userEmail);
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

        var existingMember = await _workspaceContext.WorkspaceMembers
            .FirstOrDefaultAsync(m =>
                m.WorkspaceId == invitation.WorkspaceId &&
                m.UserId == userId, ct);

        var hasExtras = invitation.IsResourceInvitation || invitation.InitialEntries.Count > 0;

        // WSP-009 / WSP-026: every check that can reject runs BEFORE the first
        // mutation. A Failure result is still committed by the data session, so
        // only an exception (or an earlier return) leaves state unchanged.
        if (hasExtras && existingMember is not null && existingMember.Status != WorkspaceMemberStatus.Active)
            return Result<AcceptInvitationResultDto>.Failure(
                "Your membership in this workspace is not active.");

        if (invitation.InitialEntries.Count > 0
            && !await _initialEntries.AllTargetsEligibleAsync(invitation.WorkspaceId, invitation.InitialEntries, ct))
        {
            return Result<AcceptInvitationResultDto>.Failure(
                "A team or space named by this invitation is no longer available. Ask for a new invitation.");
        }

        if (invitation.ResourceGrant is { } grant)
        {
            // Governance creates the explicit entry for the guest's User identity,
            // capped at Editor (GOV-030); an existing entry is reused, never lowered.
            var outcome = await _guestResourceAccess.EnsureGuestEntryAsync(
                new GuestResourceEntryRequest(
                    workspace.AccountId,
                    invitation.WorkspaceId,
                    grant.ResourceKind,
                    grant.ResourceId,
                    userId,
                    grant.Level,
                    invitation.InvitedBy,
                    now),
                ct);

            if (outcome == GuestResourceEntryOutcome.ResourceUnavailable)
                return Result<AcceptInvitationResultDto>.Failure(
                    "The shared resource is no longer available.");

            await _auditService.RecordAsync(
                invitation.WorkspaceId,
                userId,
                "AcceptResourceInvitation",
                ResourceRef.Create(ResourceKind.Create(grant.ResourceKind), grant.ResourceId, invitation.WorkspaceId),
                AuditMetadata.Create(),
                AuditSeverity.Info,
                cancellationToken: ct);
        }

        if (existingMember is not null)
        {
            invitation.Accept(userId, now);
            if (invitation.InitialEntries.Count > 0)
                await _initialEntries.ApplyAsync(
                    invitation.WorkspaceId, userId, existingMember.Id, invitation.InvitedBy,
                    invitation.InitialEntries, now, ct);

            return Result<AcceptInvitationResultDto>.Success(
                ResultFor(workspace, invitation));
        }

        await _accountMembershipActions
            .EnsureAccountMembershipAsync(
                workspace.AccountId,
                userId,
                invitation.InvitedBy,
                now,
                ct);

        invitation.Accept(userId, now);

        var member = WorkspaceMember.Create(
            workspace.AccountId,
            invitation.WorkspaceId,
            userId,
            invitation.Role,
            invitation.InvitedBy,
            now);

        _workspaceContext.WorkspaceMembers.Add(member);

        await _grantProjection.SyncWorkspaceMemberGrantAsync(
            workspace.AccountId,
            invitation.WorkspaceId,
            userId,
            invitation.Role,
            now,
            ct);

        if (invitation.InitialEntries.Count > 0)
            await _initialEntries.ApplyAsync(
                invitation.WorkspaceId, userId, member.Id, invitation.InvitedBy,
                invitation.InitialEntries, now, ct);

        return Result<AcceptInvitationResultDto>.Success(
            ResultFor(workspace, invitation));
    }

    private static AcceptInvitationResultDto ResultFor(Workspace workspace, WorkspaceInvitation invitation) =>
        new(workspace.Slug, invitation.WorkspaceId, invitation.ResourceGrant?.ResourceKind, invitation.ResourceGrant?.ResourceId);

    private static string NormalizeEmail(string email)
        => email.Trim().ToLowerInvariant();
}
