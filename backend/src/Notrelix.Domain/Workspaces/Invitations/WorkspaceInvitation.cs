using Notrelix.Domain.Workspaces.Invitations.Events;
using Notrelix.Domain.Workspaces.Members;
namespace Notrelix.Domain.Workspaces.Invitations;

public class WorkspaceInvitation : SoftDeletableAggregateRoot, IWorkspaceScoped
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
        TimeSpan? expiry = null)
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

        if (expiry is not null && expiry <= TimeSpan.Zero)
            throw new BusinessRuleException(WorkspaceRuleCodes.Workspaces_Invitation_ExpiryMustBePositive, "Invitation expiry must be greater than zero.");

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
            InvitedBy = invitedBy
        };

        invitation.SetAuditOnCreate(invitedBy, createdAt);
        invitation.RaiseDomainEvent(new WorkspaceInvitationCreatedDomainEvent(
            accountId, invitation.Id, workspaceId, invitation.Email, role, invitedBy, createdAt));

        return invitation;
    }

    public void Accept(Guid acceptedUserId, DateTimeOffset acceptedAt)
    {
        EnsureNotDeleted();
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

    public void Decline(Guid declinedBy, DateTimeOffset declinedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(declinedBy);

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
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);

        var audit = PrepareAuditUpdate(updatedBy, updatedAt);

        if (Status != WorkspaceInvitationStatus.Pending)
            throw new BusinessRuleException(WorkspaceRuleCodes.Workspaces_Invitation_NotPending, "Invitation is not pending.");

        if (newRole == WorkspaceRole.Owner)
            throw new BusinessRuleException(
                WorkspaceRuleCodes.Workspaces_Invitation_CannotInviteAsOwner,
                "Cannot invite a user as workspace owner.");

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
        EnsureNotDeleted();

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
        EnsureNotDeleted();
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
        EnsureNotDeleted();
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

    public void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        Guard.NotEmpty(deletedBy);
        if (IsDeleted) return;

        var audit = PrepareAuditUpdate(deletedBy, deletedAt);

        if (!MarkDeleted(deletedBy, deletedAt, reason)) return;
        ApplyAuditUpdate(audit);
        IncrementVersion();
        RaiseDomainEvent(new WorkspaceInvitationSoftDeletedDomainEvent(AccountId, Id, WorkspaceId, deletedBy, deletedAt));
    }

    public void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        Guard.NotEmpty(restoredBy);
        if (!IsDeleted) return;

        var audit = PrepareAuditUpdate(restoredBy, restoredAt);

        if (!MarkRestored(restoredBy, restoredAt)) return;
        ApplyAuditUpdate(audit);
        IncrementVersion();
        RaiseDomainEvent(new WorkspaceInvitationRestoredDomainEvent(AccountId, Id, WorkspaceId, restoredBy, restoredAt));
    }
}
