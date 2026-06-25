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
        method.AddDomainEvent(new UserMfaMethodAddedDomainEvent(method.Id, userId, type, createdAt));

        return method;
    }

    public void Verify(DateTimeOffset verifiedAt)
    {
        EnsureNotDeleted();
        if (Status == MfaMethodStatus.Active) return;

        if (Status == MfaMethodStatus.Disabled)
        {
            throw new BusinessRuleException("Cannot verify a disabled MFA method.");
        }

        Status = MfaMethodStatus.Active;
        VerifiedAt = verifiedAt;
        SetAuditOnUpdate(UserId, verifiedAt);

        AddDomainEvent(new UserMfaMethodVerifiedDomainEvent(Id, UserId, Type, verifiedAt));
    }

    public void SetAsPrimary(DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        if (Status != MfaMethodStatus.Active)
        {
            throw new BusinessRuleException("Only verified and active MFA methods can be set as primary.");
        }

        if (IsPrimary) return;

        IsPrimary = true;
        SetAuditOnUpdate(UserId, updatedAt);

        AddDomainEvent(new UserMfaMethodSetAsPrimaryDomainEvent(Id, UserId, Type, updatedAt));
    }

    public void UnsetAsPrimary(DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        if (!IsPrimary) return;

        IsPrimary = false;
        SetAuditOnUpdate(UserId, updatedAt);

        AddDomainEvent(new UserMfaMethodUnsetAsPrimaryDomainEvent(Id, UserId, Type, updatedAt));
    }

    public void Disable(DateTimeOffset disabledAt)
    {
        EnsureNotDeleted();
        if (Status == MfaMethodStatus.Disabled) return;

        Status = MfaMethodStatus.Disabled;
        IsPrimary = false;
        DisabledAt = disabledAt;
        SetAuditOnUpdate(UserId, disabledAt);

        AddDomainEvent(new UserMfaMethodDisabledDomainEvent(Id, UserId, Type, disabledAt));
    }

    public override void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        if (IsDeleted) return;
        base.SoftDelete(deletedBy, deletedAt, reason);
        SetAuditOnUpdate(deletedBy, deletedAt);
        IncrementVersion();
        AddDomainEvent(new UserMfaMethodSoftDeletedDomainEvent(Id, UserId, deletedBy, deletedAt, reason));
    }

    public override void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        if (!IsDeleted) return;
        base.Restore(restoredBy, restoredAt);
        SetAuditOnUpdate(restoredBy, restoredAt);
        IncrementVersion();
        AddDomainEvent(new UserMfaMethodRestoredDomainEvent(Id, UserId, restoredBy, restoredAt));
    }
}