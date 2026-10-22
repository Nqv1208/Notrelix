namespace Notrelix.Application.Features.Identity.Auth.Throttling;

/// <summary>
/// Brute-force limits for the anonymous credential steps (password login and
/// MFA challenge completion). The values are PRODUCT policy that is not yet
/// decided by the owner (docs/product/contexts/identity.md): the defaults are
/// deliberately conservative placeholders and every value is overridable
/// through configuration (<see cref="SectionName"/>). They are validated at
/// startup so a misconfiguration cannot silently disable the protection.
/// </summary>
public sealed class LoginThrottleOptions
{
    public const string SectionName = "Security:LoginThrottle";

    /// <summary>Failed attempts allowed per account (login email, or user for the MFA step) inside <see cref="AccountWindow"/>.</summary>
    public int MaxFailuresPerAccount { get; set; } = 5;

    public TimeSpan AccountWindow { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>Failed attempts allowed per client IP inside <see cref="IpWindow"/>, across all accounts.</summary>
    public int MaxFailuresPerIp { get; set; } = 20;

    public TimeSpan IpWindow { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>How long an account or an IP stays locked once its limit is reached.</summary>
    public TimeSpan LockDuration { get; set; } = TimeSpan.FromMinutes(15);
}
