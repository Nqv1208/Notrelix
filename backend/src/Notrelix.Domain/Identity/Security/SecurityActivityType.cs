namespace Notrelix.Domain.Identity.Security;

/// <summary>
/// The kinds of security-relevant account facts the user can see in their own security activity feed.
/// The names are persisted as text and exposed in the public contract: add values, never rename.
/// </summary>
public enum SecurityActivityType
{
    SignInSucceeded,
    SignInFailed,
    PasswordChanged,
    PasswordReset,
    MfaEnabled,
    MfaDisabled,
    SessionRevoked,
    OtherSessionsRevoked
}
