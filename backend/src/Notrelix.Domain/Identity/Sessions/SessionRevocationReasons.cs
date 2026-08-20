namespace Notrelix.Domain.Identity.Sessions;

/// <summary>
/// Stable, non-secret reason vocabulary for session revocation facts
/// (Phase 11 IA-SEC-003). Values are reused as the
/// <see cref="Events.UserSessionRevokedDomainEvent"/> reason and must never
/// carry tokens, passwords, IP-derived suspicion details or exception text.
/// </summary>
public static class SessionRevocationReasons
{
    public const string UserRequested = "user-requested";
    public const string UserRevokedOtherSessions = "user-revoked-other-sessions";
    public const string PasswordChanged = "password-changed";
    public const string PasswordReset = "password-reset";
    public const string MfaDisabled = "mfa-disabled";
    public const string UserDeactivated = "user-deactivated";
    public const string SecurityRecovery = "security-recovery";
}