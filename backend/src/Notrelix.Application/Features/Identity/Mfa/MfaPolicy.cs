namespace Notrelix.Application.Features.Identity.Mfa;

/// <summary>
/// Central MFA policy values shared by challenge and enrollment use cases.
/// </summary>
public static class MfaPolicy
{
    /// <summary>Lifetime of a single-use challenge issued after credential verification.</summary>
    public static readonly TimeSpan ChallengeTtl = TimeSpan.FromMinutes(5);

    /// <summary>Number of one-time recovery codes issued per batch.</summary>
    public const int RecoveryCodeCount = 8;

    /// <summary>Time step (seconds) for RFC 6238 TOTP codes.</summary>
    public const int TotpTimeStepSeconds = 30;

    /// <summary>Accepted drift, in time steps, on either side of the current step.</summary>
    public const int TotpAllowedDriftSteps = 1;

    /// <summary>Issuer label used in the otpauth:// URI.</summary>
    public const string TotpIssuer = "Notrelix";
}
