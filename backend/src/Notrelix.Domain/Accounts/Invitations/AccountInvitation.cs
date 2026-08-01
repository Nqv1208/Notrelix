using Notrelix.Domain.Accounts.Invitations.Events;
using Notrelix.Domain.Accounts.Members;

namespace Notrelix.Domain.Accounts.Invitations;

public class AccountInvitation : AggregateRoot, IAccountScoped
{
    public Guid AccountId { get; private set; }
    public string Email { get; private set; } = null!;
    public AccountRole Role { get; private set; }
    public AccountInvitationStatus Status { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public Guid InvitedBy { get; private set; }

    private AccountInvitation() : base() { }

    public static AccountInvitation Create(
        Guid accountId,
        string email,
        AccountRole role,
        Guid invitedBy,
        DateTimeOffset createdAt,
        TimeSpan? expiry = null)
    {
        Guard.NotEmpty(accountId);
        Guard.NotNullOrWhiteSpace(email);
        Guard.NotEmpty(invitedBy);

        var emailValue = SharedKernel.Email.Create(email);

        if (expiry is not null && expiry <= TimeSpan.Zero)
            throw new BusinessRuleException(AccountRuleCodes.Accounts_Invitation_ExpiryMustBePositive, "Invitation expiry must be greater than zero.");

        var invitation = new AccountInvitation
        {
            AccountId = accountId,
            Email = emailValue.Value,
            Role = role,
            Status = AccountInvitationStatus.Pending,
            ExpiresAt = createdAt.Add(expiry ?? TimeSpan.FromDays(7)),
            InvitedBy = invitedBy
        };

        invitation.SetAuditOnCreate(invitedBy, createdAt);
        invitation.RaiseDomainEvent(new AccountInvitationCreatedDomainEvent(
            invitation.Id, accountId, invitation.Email, role, invitedBy, createdAt));

        return invitation;
    }

    public void Accept(Guid acceptedUserId, DateTimeOffset acceptedAt)
    {
        Guard.NotEmpty(acceptedUserId);

        if (Status != AccountInvitationStatus.Pending)
            throw new BusinessRuleException(AccountRuleCodes.Accounts_Invitation_NotPending, "Invitation is not pending.");

        if (acceptedAt >= ExpiresAt)
            throw new BusinessRuleException(AccountRuleCodes.Accounts_Invitation_HasExpired, "Invitation has expired.");

        var pending = PrepareAuditUpdate(acceptedUserId, acceptedAt);
        Status = AccountInvitationStatus.Accepted;
        ApplyAuditUpdate(pending);
        IncrementVersion();

        RaiseDomainEvent(new AccountInvitationAcceptedDomainEvent(
            Id, AccountId, acceptedUserId, acceptedUserId, acceptedAt));
    }

    public void Expire(DateTimeOffset expiredAt)
    {
        if (Status != AccountInvitationStatus.Pending) return;

        var pending = PrepareAuditUpdate(null, expiredAt);
        Status = AccountInvitationStatus.Expired;
        ApplyAuditUpdate(pending);
        IncrementVersion();

        RaiseDomainEvent(new AccountInvitationExpiredDomainEvent(Id, AccountId, expiredAt));
    }

    public void Revoke(Guid revokedBy, DateTimeOffset revokedAt)
    {
        Guard.NotEmpty(revokedBy);

        if (Status != AccountInvitationStatus.Pending) return;

        if (revokedAt >= ExpiresAt)
            throw new BusinessRuleException(AccountRuleCodes.Accounts_Invitation_HasExpired, "Invitation has expired.");

        var pending = PrepareAuditUpdate(revokedBy, revokedAt);
        Status = AccountInvitationStatus.Revoked;
        ApplyAuditUpdate(pending);
        IncrementVersion();

        RaiseDomainEvent(new AccountInvitationRevokedDomainEvent(Id, AccountId, revokedBy, revokedAt));
    }
}
