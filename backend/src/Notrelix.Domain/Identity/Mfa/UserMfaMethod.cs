using Notrelix.Domain.Identity.Mfa.Events;

namespace Notrelix.Domain.Identity.Mfa;

public sealed class UserMfaMethod : AggregateRoot
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
        if (Status == MfaMethodStatus.Active) return;

        if (Status == MfaMethodStatus.Disabled)
        {
            throw new BusinessRuleException(IdentityRuleCodes.Identity_Mfa_CannotVerifyDisabled, "Cannot verify a disabled MFA method.");
        }

        var pending = PrepareAuditUpdate(UserId, verifiedAt);
        Status = MfaMethodStatus.Active;
        VerifiedAt = verifiedAt;
        ApplyAuditUpdate(pending);
        IncrementVersion();

        RaiseDomainEvent(new UserMfaMethodVerifiedDomainEvent(Id, UserId, Type, verifiedAt));
    }

    public void SetAsPrimary(DateTimeOffset updatedAt)
    {
        if (Status != MfaMethodStatus.Active)
        {
            throw new BusinessRuleException(IdentityRuleCodes.Identity_Mfa_CannotSetPrimaryUnlessVerifiedActive, "Only verified and active MFA methods can be set as primary.");
        }

        if (IsPrimary) return;

        var pending = PrepareAuditUpdate(UserId, updatedAt);
        IsPrimary = true;
        ApplyAuditUpdate(pending);
        IncrementVersion();

        RaiseDomainEvent(new UserMfaMethodSetAsPrimaryDomainEvent(Id, UserId, Type, updatedAt));
    }

    public void UnsetAsPrimary(DateTimeOffset updatedAt)
    {
        if (!IsPrimary) return;

        var pending = PrepareAuditUpdate(UserId, updatedAt);
        IsPrimary = false;
        ApplyAuditUpdate(pending);
        IncrementVersion();

        RaiseDomainEvent(new UserMfaMethodUnsetAsPrimaryDomainEvent(Id, UserId, Type, updatedAt));
    }

    public void Disable(DateTimeOffset disabledAt)
    {
        if (Status == MfaMethodStatus.Disabled) return;

        var pending = PrepareAuditUpdate(UserId, disabledAt);
        Status = MfaMethodStatus.Disabled;
        IsPrimary = false;
        DisabledAt = disabledAt;
        ApplyAuditUpdate(pending);
        IncrementVersion();

        RaiseDomainEvent(new UserMfaMethodDisabledDomainEvent(Id, UserId, Type, disabledAt));
    }
}
