using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.Identity.Mfa.Events;

namespace Notrelix.Domain.Identity.Mfa;

public class UserMfaMethod : AggregateRoot
{
    public Guid UserId { get; private set; }
    public MfaMethodType Type { get; private set; }
    public string? SecretRef { get; private set; }
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
        string? secretRef = null, 
        string? destinationMasked = null, 
        bool isPrimary = false)
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
            IsPrimary = isPrimary
        };

        method.SetAuditOnCreate(userId, createdAt);
        method.AddDomainEvent(new UserMfaMethodAddedEvent(method.Id, userId, type, createdAt));

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

        AddDomainEvent(new UserMfaMethodVerifiedEvent(Id, UserId, Type, verifiedAt));
    }

    public void SetPrimary(bool isPrimary, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        if (isPrimary && Status != MfaMethodStatus.Active)
        {
            throw new BusinessRuleException("Only verified and active MFA methods can be set as primary.");
        }

        IsPrimary = isPrimary;
        SetAuditOnUpdate(UserId, updatedAt);
        
        AddDomainEvent(new UserMfaMethodSetAsPrimaryEvent(Id, UserId, Type, updatedAt));
    }

    public void Disable(DateTimeOffset disabledAt)
    {
        EnsureNotDeleted();
        if (Status == MfaMethodStatus.Disabled) return;

        Status = MfaMethodStatus.Disabled;
        IsPrimary = false;
        DisabledAt = disabledAt;
        SetAuditOnUpdate(UserId, disabledAt);

        AddDomainEvent(new UserMfaMethodDisabledEvent(Id, UserId, Type, disabledAt));
    }
}