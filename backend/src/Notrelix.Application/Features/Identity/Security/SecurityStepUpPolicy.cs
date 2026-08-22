namespace Notrelix.Application.Features.Identity.Security;

/// <summary>
/// Central step-up proof policy values. Kept separate from MFA policy so that
/// challenge-policy changes cannot accidentally alter verified-proof lifetime.
/// </summary>
public static class SecurityStepUpPolicy
{
    /// <summary>Lifetime of a verified single-use step-up proof token.</summary>
    public static readonly TimeSpan ProofTtl = TimeSpan.FromMinutes(5);
}
