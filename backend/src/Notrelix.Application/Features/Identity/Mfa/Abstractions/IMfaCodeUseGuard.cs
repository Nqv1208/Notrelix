namespace Notrelix.Application.Features.Identity.Mfa.Abstractions;

/// <summary>
/// Makes a second-factor credential work once. A TOTP code is good for one time step (RFC 6238
/// section 5.2: a verifier must not accept a second attempt of a code that already succeeded), and a
/// recovery code is good once even when two requests present it at the same moment. The first claimant
/// wins; every later one is refused.
/// </summary>
public interface IMfaCodeUseGuard
{
    /// <returns><c>true</c> for the first claim of this person's code for that time step.</returns>
    Task<bool> TryClaimTotpStepAsync(Guid userId, long timeStep, CancellationToken ct = default);

    /// <returns><c>true</c> for the first claim of this recovery code (by its hash).</returns>
    Task<bool> TryClaimRecoveryCodeAsync(Guid userId, string codeHash, CancellationToken ct = default);
}
