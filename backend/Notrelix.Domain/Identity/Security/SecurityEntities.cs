using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Identity.Security;

public class UserLoginAttempt : Entity
{
    public Guid? UserId { get; private set; }
    public string? Email { get; private set; }
    public bool Succeeded { get; private set; }
    public string? FailureReason { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }

    private UserLoginAttempt() : base() { }

    public static UserLoginAttempt Record(Guid? userId, string? email, bool succeeded, DateTimeOffset occurredAt, string? failureReason = null, string? ipAddress = null, string? userAgent = null)
    {
        var attempt = new UserLoginAttempt
        {
            UserId = userId,
            Email = email,
            Succeeded = succeeded,
            FailureReason = failureReason,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            OccurredAt = occurredAt
        };

        attempt.AddDomainEvent(new LoginAttemptRecordedEvent(userId, email, succeeded, occurredAt));
        return attempt;
    }
}

public class UserMfaMethod : AggregateRoot
{
    public Guid UserId { get; private set; }
    public MfaMethodType Type { get; private set; }
    public string? SecretRef { get; private set; }
    public string? DestinationMasked { get; private set; }
    public bool IsVerified { get; private set; }
    public bool IsPrimary { get; private set; }
    public DateTimeOffset? VerifiedAt { get; private set; }
    public DateTimeOffset? DisabledAt { get; private set; }

    private UserMfaMethod() : base() { }

    public static UserMfaMethod Create(Guid userId, MfaMethodType type, DateTimeOffset createdAt, string? secretRef = null, string? destinationMasked = null, bool isPrimary = false)
    {
        Guard.NotEmpty(userId);

        var method = new UserMfaMethod
        {
            UserId = userId,
            Type = type,
            SecretRef = secretRef,
            DestinationMasked = destinationMasked,
            IsVerified = false,
            IsPrimary = isPrimary
        };

        method.SetAuditOnCreate(userId, createdAt);
        return method;
    }

    public void Verify(DateTimeOffset verifiedAt)
    {
        IsVerified = true;
        VerifiedAt = verifiedAt;
        SetAuditOnUpdate(UserId, verifiedAt);
    }

    public void SetPrimary(bool isPrimary)
    {
        IsPrimary = isPrimary;
    }

    public void Disable(DateTimeOffset disabledAt)
    {
        DisabledAt = disabledAt;
        IsPrimary = false;
        SetAuditOnUpdate(UserId, disabledAt);
    }
}

public class UserSecuritySettings : Entity
{
    public Guid UserId { get; private set; }
    public bool IsMfaEnabled { get; private set; }
    public MfaMethodType? PreferredMfaMethod { get; private set; }
    public bool RequirePasswordChange { get; private set; }
    public DateTimeOffset? PasswordChangedAt { get; private set; }
    public DateTimeOffset? LastSecurityReviewAt { get; private set; }
    public JsonValue SettingsJson { get; private set; } = null!;

    private UserSecuritySettings() : base() { }

    public static UserSecuritySettings Create(Guid userId)
    {
        Guard.NotEmpty(userId);
        return new UserSecuritySettings
        {
            UserId = userId,
            IsMfaEnabled = false,
            SettingsJson = JsonValue.Create("{}")
        };
    }

    public void EnableMfa(MfaMethodType method, DateTimeOffset updatedAt)
    {
        IsMfaEnabled = true;
        PreferredMfaMethod = method;
        LastSecurityReviewAt = updatedAt;
        AddDomainEvent(new MfaEnabledEvent(UserId, method, updatedAt));
    }

    public void DisableMfa(DateTimeOffset updatedAt)
    {
        var previousMethod = PreferredMfaMethod;
        IsMfaEnabled = false;
        PreferredMfaMethod = null;
        LastSecurityReviewAt = updatedAt;
        AddDomainEvent(new MfaDisabledEvent(UserId, previousMethod ?? MfaMethodType.AuthenticatorApp, updatedAt));
    }

    public void RequirePasswordChangeNow(DateTimeOffset updatedAt)
    {
        RequirePasswordChange = true;
        LastSecurityReviewAt = updatedAt;
    }

    public void PasswordChanged(DateTimeOffset updatedAt)
    {
        RequirePasswordChange = false;
        PasswordChangedAt = updatedAt;
        LastSecurityReviewAt = updatedAt;
    }

    public void UpdateSettings(JsonValue settings, DateTimeOffset updatedAt)
    {
        Guard.NotNull(settings);
        SettingsJson = settings;
        LastSecurityReviewAt = updatedAt;
    }
}
