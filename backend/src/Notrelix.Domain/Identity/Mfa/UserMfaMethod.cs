using Notrelix.Domain.Identity.Mfa.Events;

namespace Notrelix.Domain.Identity.Mfa;

public class UserMfaMethod : AggregateRoot
{
    public Guid UserId { get; private set; }
    public MfaMethodType Type { get; private set; }
    public SecretRef? SecretRef { get; private set; }
    public string? DestinationMasked { get; private set; }
    public MfaMethodStatus Status { get; private set; }
    public bool IsPrimary { get; private set; }
    public DateTimeOffset? VerifiedAt { get; private set; }
    public DateTimeOffset? DisabledAt { get; private set; }

    public bool IsVerified => Status == MfaMethodStatus.Active;

    private UserMfaMethod() : base() { }

    public static UserMfaMethod Create(
        Guid userId,
        MfaMethodType type,
        DateTimeOffset createdAt,
        SecretRef? secretRef = null,
        string? destinationMasked = null)
    {
        Guard.NotEmpty(userId);
        MfaMethodRules.EnsureValidCreation(type, secretRef, destinationMasked);

        var method = new UserMfaMethod
        {
            UserId = userId,
            Type = type,
            SecretRef = secretRef,
            DestinationMasked = destinationMasked,
            Status = MfaMethodStatus.PendingVerification,
            IsPrimary = false
        };

        method.SetAuditOnCreate(userId, createdAt);
        method.RaiseDomainEvent(new UserMfaMethodAddedDomainEvent(method.Id, userId, type, createdAt));

        return method;
    }

    public void Verify(DateTimeOffset verifiedAt)
    {
        EnsureNotDeleted();
        if (Status == MfaMethodStatus.Active) return;

        if (Status == MfaMethodStatus.Disabled)
        {
            throw new BusinessRuleException(BusinessRuleCodes.Identity_Mfa_CannotVerifyDisabled, "Cannot verify a disabled MFA method.");
        }

        Status = MfaMethodStatus.Active;
        VerifiedAt = verifiedAt;
        SetAuditOnUpdate(UserId, verifiedAt);
        IncrementVersion();

        RaiseDomainEvent(new UserMfaMethodVerifiedDomainEvent(Id, UserId, Type, verifiedAt));
    }

    public void SetAsPrimary(DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        if (Status != MfaMethodStatus.Active)
        {
            throw new BusinessRuleException(BusinessRuleCodes.Identity_Mfa_CannotSetPrimaryUnlessVerifiedActive, "Only verified and active MFA methods can be set as primary.");
        }

        if (IsPrimary) return;

        IsPrimary = true;
        SetAuditOnUpdate(UserId, updatedAt);
        IncrementVersion();

        RaiseDomainEvent(new UserMfaMethodSetAsPrimaryDomainEvent(Id, UserId, Type, updatedAt));
    }

    public void UnsetAsPrimary(DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        if (!IsPrimary) return;

        IsPrimary = false;
        SetAuditOnUpdate(UserId, updatedAt);
        IncrementVersion();

        RaiseDomainEvent(new UserMfaMethodUnsetAsPrimaryDomainEvent(Id, UserId, Type, updatedAt));
    }

    public void Disable(DateTimeOffset disabledAt)
    {
        EnsureNotDeleted();
        if (Status == MfaMethodStatus.Disabled) return;

        Status = MfaMethodStatus.Disabled;
        IsPrimary = false;
        DisabledAt = disabledAt;
        SetAuditOnUpdate(UserId, disabledAt);
        IncrementVersion();

        RaiseDomainEvent(new UserMfaMethodDisabledDomainEvent(Id, UserId, Type, disabledAt));
    }

    public void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        if (IsDeleted) return;
        if (!MarkDeleted(deletedBy, deletedAt, reason)) return;
        SetAuditOnUpdate(deletedBy, deletedAt);
        IncrementVersion();
        RaiseDomainEvent(new UserMfaMethodSoftDeletedDomainEvent(Id, UserId, deletedBy, deletedAt, reason));
    }

    public void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        if (!IsDeleted) return;
        if (!MarkRestored(restoredBy, restoredAt)) return;
        SetAuditOnUpdate(restoredBy, restoredAt);
        IncrementVersion();
        RaiseDomainEvent(new UserMfaMethodRestoredDomainEvent(Id, UserId, restoredBy, restoredAt));
    }
}