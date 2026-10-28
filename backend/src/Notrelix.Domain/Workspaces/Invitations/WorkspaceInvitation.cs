using Notrelix.Domain.Workspaces.Invitations.Events;
using Notrelix.Domain.Workspaces.Members;
namespace Notrelix.Domain.Workspaces.Invitations;

public class WorkspaceInvitation : AggregateRoot, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public string Email { get; private set; } = null!;
    public WorkspaceRole Role { get; private set; }
    public InvitationTokenHash Token { get; private set; } = null!;
    public int HashVersion { get; private set; }
    public int TokenGeneration { get; private set; }
    public WorkspaceInvitationStatus Status { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public Guid InvitedBy { get; private set; }

    /// <summary>Initial Team/Space entries applied atomically on acceptance (WSP-026).</summary>
    public IReadOnlyList<WorkspaceInvitationInitialEntry> InitialEntries { get; private set; } = [];

    /// <summary>
    /// Set for a resource invitation (WSP-026 / GOV-030): acceptance creates or
    /// reuses a Guest membership plus this explicit entry on that one resource.
    /// </summary>
    public WorkspaceInvitationResourceGrant? ResourceGrant { get; private set; }

    public bool IsResourceInvitation => ResourceGrant is not null;

    private WorkspaceInvitation() : base() { }

    public static WorkspaceInvitation Create(
        Guid accountId,
        Guid workspaceId,
        string email,
        WorkspaceRole role,
        InvitationTokenHash token,
        int hashVersion,
        Guid invitedBy,
        DateTimeOffset createdAt,
        TimeSpan? expiry = null,
        IReadOnlyList<WorkspaceInvitationInitialEntry>? initialEntries = null)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(accountId);
        Guard.NotNullOrWhiteSpace(email);
        Guard.NotNull(token);
        Guard.NotEmpty(invitedBy);

        var emailValue = SharedKernel.Email.Create(email);

        if (role == WorkspaceRole.Owner)
            throw new BusinessRuleException(
                WorkspaceRuleCodes.Workspaces_Invitation_CannotInviteAsOwner,
                "Cannot invite a user as workspace owner.");

        EnsureNotAdmin(role);

        if (expiry is not null && expiry <= TimeSpan.Zero)
            throw new BusinessRuleException(WorkspaceRuleCodes.Workspaces_Invitation_ExpiryMustBePositive, "Invitation expiry must be greater than zero.");

        var entries = NormalizeEntries(role, initialEntries);

        var invitation = new WorkspaceInvitation
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            Email = emailValue.Value,
            Role = role,
            Token = token,
            HashVersion = hashVersion,
            TokenGeneration = 1,
            Status = WorkspaceInvitationStatus.Pending,
            ExpiresAt = createdAt.Add(expiry ?? TimeSpan.FromDays(7)),
            InvitedBy = invitedBy,
            InitialEntries = entries
        };

        invitation.SetAuditOnCreate(invitedBy, createdAt);
        invitation.RaiseDomainEvent(new WorkspaceInvitationCreatedDomainEvent(
            accountId, invitation.Id, workspaceId, invitation.Email, role, invitedBy, createdAt));

        return invitation;
    }

    /// <summary>
    /// A resource invitation by email (WSP-026): always a Guest invitation, never
    /// carries Team/Space entries, and names the one resource entry created on
    /// acceptance.
    /// </summary>
    public static WorkspaceInvitation CreateForResource(
        Guid accountId,
        Guid workspaceId,
        string email,
        WorkspaceInvitationResourceGrant grant,
        InvitationTokenHash token,
        int hashVersion,
        Guid invitedBy,
        DateTimeOffset createdAt,
        TimeSpan? expiry = null)
    {
        Guard.NotNull(grant);
        var invitation = Create(accountId, workspaceId, email, WorkspaceRole.Guest, token, hashVersion, invitedBy, createdAt, expiry);
        invitation.ResourceGrant = grant;
        return invitation;
    }

    // WSP-026: a workspace invitation offers Member or Guest only. The rule guards
    // every new or changed role; an Admin invitation issued before the rule stays
    // acceptable as stored (Accept never re-checks the role), so no data migration
    // is needed and no outstanding invitation is invalidated.
    private static void EnsureNotAdmin(WorkspaceRole role)
    {
        if (role == WorkspaceRole.Admin)
            throw new BusinessRuleException(
                WorkspaceRuleCodes.Workspaces_Invitation_CannotInviteAsAdmin,
                "A workspace invitation offers Member or Guest only; promote the member after they join.");
    }

    private static IReadOnlyList<WorkspaceInvitationInitialEntry> NormalizeEntries(
        WorkspaceRole role,
        IReadOnlyList<WorkspaceInvitationInitialEntry>? entries)
    {
        if (entries is null || entries.Count == 0) return [];

        if (role is not (WorkspaceRole.Member or WorkspaceRole.Guest))
            throw new BusinessRuleException(
                WorkspaceRuleCodes.Workspaces_Invitation_InitialEntriesRequireMemberOrGuest,
                "Initial Team and Space entries can be named only for a Member or Guest invitation.");

        if (entries.Count > WorkspaceInvitationInitialEntry.MaxEntries)
            throw new BusinessRuleException(
                WorkspaceRuleCodes.Workspaces_Invitation_TooManyInitialEntries,
                $"An invitation can name at most {WorkspaceInvitationInitialEntry.MaxEntries} initial entries.");

        if (entries.Select(e => (e.Kind, e.TargetId)).Distinct().Count() != entries.Count)
            throw new BusinessRuleException(
                WorkspaceRuleCodes.Workspaces_Invitation_InitialEntryDuplicate,
                "An initial entry names a Team or Space only once.");

        EnsureGuestEntryRoles(role, entries);

        return entries.ToArray();
    }

    // A Guest never manages: no Team lead and no Space admin entry for a Guest
    // invitation (GOV-030 keeps a Guest from managing access).
    private static void EnsureGuestEntryRoles(WorkspaceRole role, IReadOnlyList<WorkspaceInvitationInitialEntry> entries)
    {
        if (role != WorkspaceRole.Guest) return;

        if (entries.Any(e => e.Role is nameof(Teams.TeamMemberRole.Lead) or nameof(Spaces.SpaceRole.Admin)))
            throw new BusinessRuleException(
                WorkspaceRuleCodes.Workspaces_Invitation_GuestEntryRoleTooPrivileged,
                "A Guest invitation cannot name a Team lead or Space admin entry.");
    }

    public void Accept(Guid acceptedUserId, DateTimeOffset acceptedAt)
    {
        Guard.NotEmpty(acceptedUserId);

        var audit = PrepareAuditUpdate(acceptedUserId, acceptedAt);

        if (Status == WorkspaceInvitationStatus.Accepted) return;

        if (Status != WorkspaceInvitationStatus.Pending)
            throw new BusinessRuleException(WorkspaceRuleCodes.Workspaces_Invitation_NotPending, "Invitation is not pending.");

        if (acceptedAt >= ExpiresAt)
            throw new BusinessRuleException(WorkspaceRuleCodes.Workspaces_Invitation_HasExpired, "Invitation has expired.");

        Status = WorkspaceInvitationStatus.Accepted;
        ApplyAuditUpdate(audit);
        IncrementVersion();

        RaiseDomainEvent(new WorkspaceInvitationAcceptedDomainEvent(
            AccountId, Id, WorkspaceId, acceptedUserId, acceptedUserId, acceptedAt));
    }

    /// <summary>
    /// The invitee declines. <paramref name="declinedBy"/> is the signed-in user, or null when the
    /// holder of the one-time token declines without an identity (a person with no account).
    /// Only a pending invitation can be declined.
    /// </summary>
    public void Decline(Guid? declinedBy, DateTimeOffset declinedAt)
    {
        if (declinedBy is { } actor)
            Guard.NotEmpty(actor);

        var audit = PrepareAuditUpdate(declinedBy, declinedAt);

        if (Status != WorkspaceInvitationStatus.Pending)
            throw new BusinessRuleException(WorkspaceRuleCodes.Workspaces_Invitation_NotPending, "Invitation is not pending.");

        Status = WorkspaceInvitationStatus.Declined;
        ApplyAuditUpdate(audit);
        IncrementVersion();

        RaiseDomainEvent(new WorkspaceInvitationDeclinedDomainEvent(
            AccountId, Id, WorkspaceId, declinedBy, declinedAt));
    }

    public void ChangeRole(WorkspaceRole newRole, Guid updatedBy, DateTimeOffset updatedAt)
    {
        Guard.NotEmpty(updatedBy);

        var audit = PrepareAuditUpdate(updatedBy, updatedAt);

        if (Status != WorkspaceInvitationStatus.Pending)
            throw new BusinessRuleException(WorkspaceRuleCodes.Workspaces_Invitation_NotPending, "Invitation is not pending.");

        if (newRole == WorkspaceRole.Owner)
            throw new BusinessRuleException(
                WorkspaceRuleCodes.Workspaces_Invitation_CannotInviteAsOwner,
                "Cannot invite a user as workspace owner.");

        EnsureNotAdmin(newRole);

        if (ResourceGrant is not null && newRole != WorkspaceRole.Guest)
            throw new BusinessRuleException(
                WorkspaceRuleCodes.Workspaces_Invitation_ResourceInvitationMustBeGuest,
                "A resource invitation never creates a member: its role stays Guest.");

        if (InitialEntries.Count > 0 && newRole is not (WorkspaceRole.Member or WorkspaceRole.Guest))
            throw new BusinessRuleException(
                WorkspaceRuleCodes.Workspaces_Invitation_InitialEntriesRequireMemberOrGuest,
                "Initial Team and Space entries can be named only for a Member or Guest invitation.");

        EnsureGuestEntryRoles(newRole, InitialEntries);

        if (Role == newRole) return;

        var oldRole = Role;
        Role = newRole;
        ApplyAuditUpdate(audit);
        IncrementVersion();

        RaiseDomainEvent(new WorkspaceInvitationRoleChangedDomainEvent(
            AccountId, Id, WorkspaceId, oldRole, newRole, updatedBy, updatedAt));
    }

    public void Expire(DateTimeOffset expiredAt)
    {
        var audit = PrepareAuditUpdate(null, expiredAt);

        if (Status != WorkspaceInvitationStatus.Pending) return;

        Status = WorkspaceInvitationStatus.Expired;
        ApplyAuditUpdate(audit);
        IncrementVersion();

        RaiseDomainEvent(new WorkspaceInvitationExpiredDomainEvent(
            AccountId, Id, WorkspaceId, expiredAt));
    }

    public void Revoke(Guid revokedBy, DateTimeOffset revokedAt)
    {
        Guard.NotEmpty(revokedBy);

        var audit = PrepareAuditUpdate(revokedBy, revokedAt);

        if (Status != WorkspaceInvitationStatus.Pending) return;

        Status = WorkspaceInvitationStatus.Revoked;
        ApplyAuditUpdate(audit);
        IncrementVersion();

        RaiseDomainEvent(new WorkspaceInvitationRevokedDomainEvent(
            AccountId, Id, WorkspaceId, revokedBy, revokedAt));
    }

    public void Resend(
        InvitationTokenHash newTokenHash,
        int newHashVersion,
        DateTimeOffset resentAt,
        TimeSpan expiry,
        Guid resentBy)
    {
        Guard.NotNull(newTokenHash);
        Guard.NotEmpty(resentBy);

        var audit = PrepareAuditUpdate(resentBy, resentAt);

        if (Status is not WorkspaceInvitationStatus.Pending and
            not WorkspaceInvitationStatus.Expired)
        {
            throw new BusinessRuleException(
                WorkspaceRuleCodes.Workspaces_Invitation_CannotResendNonPendingExpired,
                "Only pending or expired invitations can be resent.");
        }

        Token = newTokenHash;
        HashVersion = newHashVersion;
        TokenGeneration = checked(TokenGeneration + 1);
        Status = WorkspaceInvitationStatus.Pending;
        ExpiresAt = resentAt.Add(expiry);

        ApplyAuditUpdate(audit);
        IncrementVersion();

        RaiseDomainEvent(new WorkspaceInvitationResentDomainEvent(
            AccountId, Id, WorkspaceId, resentBy, resentAt));
    }
}
