namespace Notrelix.Domain.Identity.Security;

public enum LoginAttemptResult
{
    Succeeded,
    Failed,
    MfaRequired,
    Locked
}
