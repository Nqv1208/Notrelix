namespace Notrelix.Application.Features.Identity.Auth.Throttling;

/// <summary>
/// Brute-force limits for the anonymous credential steps (password login and
/// MFA challenge completion). The defaults are the product policy accepted on
/// 2026-10-23 (docs/product/contexts/identity.md): 10 failures per account and 50
/// per IP in 15 minutes, each lock lasting 15 minutes. Every value is overridable
/// through configuration (<see cref="SectionName"/>) and validated at startup so a
/// misconfiguration cannot silently disable the protection. The wrong-code budget of
/// one MFA challenge is separate and fixed (<c>MfaPolicy.ChallengeMaxAttempts</c>).
/// </summary>
public sealed class LoginThrottleOptions
{
    public const string SectionName = "Security:LoginThrottle";

    /// <summary>Failed attempts allowed per account (login email, or user for the MFA step) inside <see cref="AccountWindow"/>.</summary>
    public int MaxFailuresPerAccount { get; set; } = 10;

    public TimeSpan AccountWindow { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>Failed attempts allowed per client IP inside <see cref="IpWindow"/>, across all accounts.</summary>
    public int MaxFailuresPerIp { get; set; } = 50;

    public TimeSpan IpWindow { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>How long an account or an IP stays locked once its limit is reached.</summary>
    public TimeSpan LockDuration { get; set; } = TimeSpan.FromMinutes(15);
}
