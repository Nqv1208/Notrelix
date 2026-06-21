namespace Notrelix.Domain.Identity.Security;

public static class LoginFailureReason
{
    public const string InvalidCredentials = "InvalidCredentials";
    public const string UserNotFound = "UserNotFound";
    public const string UserSuspended = "UserSuspended";
    public const string UserInactive = "UserInactive";
    public const string MfaRequired = "MfaRequired";
    public const string InvalidMfaCode = "InvalidMfaCode";
    public const string LockedOut = "LockedOut";
    public const string RateLimitExceeded = "RateLimitExceeded";
}
