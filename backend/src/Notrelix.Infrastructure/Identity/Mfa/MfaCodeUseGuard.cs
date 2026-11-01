using Notrelix.Application.Features.Identity.Mfa;
using Notrelix.Application.Features.Identity.Mfa.Abstractions;

namespace Notrelix.Infrastructure.Identity.Mfa;

/// <summary>
/// Redis-backed claims: the first atomic increment of a key wins. A code that is accepted for ±1 time
/// step can be presented for about 90 seconds, so a claim outlives that window.
/// </summary>
public sealed class MfaCodeUseGuard : IMfaCodeUseGuard
{
    private static readonly TimeSpan TotpClaimTtl =
        TimeSpan.FromSeconds(MfaPolicy.TotpTimeStepSeconds * (2 * MfaPolicy.TotpAllowedDriftSteps + 2));

    /// <summary>The database records the consumed code; the claim only has to cover the race between two requests.</summary>
    private static readonly TimeSpan RecoveryClaimTtl = TimeSpan.FromMinutes(5);

    private readonly IRedisCacheService _cache;

    public MfaCodeUseGuard(IRedisCacheService cache)
    {
        _cache = cache;
    }

    public async Task<bool> TryClaimTotpStepAsync(Guid userId, long timeStep, CancellationToken ct) =>
        await _cache.IncrementAsync($"identity:mfa:totp-used:{userId:N}:{timeStep}", TotpClaimTtl, ct) == 1;

    public async Task<bool> TryClaimRecoveryCodeAsync(Guid userId, string codeHash, CancellationToken ct) =>
        await _cache.IncrementAsync($"identity:mfa:recovery-used:{userId:N}:{codeHash}", RecoveryClaimTtl, ct) == 1;
}
