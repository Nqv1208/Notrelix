using Notrelix.Domain.Common;

namespace Notrelix.Domain.Identity.Security;

public class LoginAttempt : Entity
{
    public Guid? UserId { get; private set; }
    public string? Email { get; private set; }
    public bool Succeeded { get; private set; }
    public string? FailureReason { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }

    private LoginAttempt() : base() { }

    public static LoginAttempt Record(Guid? userId, string? email, bool succeeded, string? failureReason = null, string? ipAddress = null, string? userAgent = null)
    {
        return new LoginAttempt
        {
            UserId = userId,
            Email = email,
            Succeeded = succeeded,
            FailureReason = failureReason,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            OccurredAt = DateTimeOffset.UtcNow
        };
    }
}

public class MfaMethod : AggregateRoot
{
    public Guid UserId { get; private set; }
    public MfaMethodType Type { get; private set; }
    public string? SecretReference { get; private set; }
    public bool IsVerified { get; private set; }
    public bool IsPrimary { get; private set; }

    private MfaMethod() : base() { }

    public static MfaMethod Create(Guid userId, MfaMethodType type, string? secretRef = null, bool isPrimary = false)
    {
        Guard.NotEmpty(userId);

        return new MfaMethod
        {
            UserId = userId,
            Type = type,
            SecretReference = secretRef,
            IsVerified = false,
            IsPrimary = isPrimary
        };
    }
}

public class UserSecuritySettings : Entity
{
    public Guid UserId { get; private set; }
    public bool IsMfaEnabled { get; private set; }
    public MfaMethodType? PreferredMfaMethod { get; private set; }
    public bool RequirePasswordChange { get; private set; }
    public DateTimeOffset? LastSecurityReviewAt { get; private set; }

    private UserSecuritySettings() : base() { }

    public static UserSecuritySettings Create(Guid userId)
    {
        return new UserSecuritySettings
        {
            UserId = userId,
            IsMfaEnabled = false
        };
    }
}
