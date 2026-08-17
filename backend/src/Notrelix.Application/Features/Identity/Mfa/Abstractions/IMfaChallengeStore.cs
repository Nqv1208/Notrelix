using Notrelix.Application.Features.Identity.Mfa.DTOs;

namespace Notrelix.Application.Features.Identity.Mfa.Abstractions;

/// <summary>
/// Single-use MFA challenge storage. Tokens are consumed exactly once;
/// only the SHA-256 of the token is used as the cache key.
/// A challenge is an UNVERIFIED token: it authorizes factor verification
/// (and invalidation), never a sensitive mutation.
/// </summary>
public interface IMfaChallengeStore
{
    Task StoreAsync(string token, MfaChallengePayload payload, TimeSpan ttl, CancellationToken ct = default);

    /// <summary>Non-destructive read used before factor verification.</summary>
    Task<MfaChallengePayload?> PeekAsync(string token, CancellationToken ct = default);

    /// <summary>Atomic get+delete used only for success finalization or explicit invalidation.</summary>
    Task<MfaChallengePayload?> ConsumeAsync(string token, CancellationToken ct = default);

    /// <summary>
    /// Atomically records one verification attempt against the challenge and
    /// invalidates the challenge (removes it) when attempts exceed
    /// <c>maxAttempts</c>. The budget is cumulative for the challenge lifetime
    /// and does not depend on rate-limiter time windows.
    /// </summary>
    Task<MfaChallengeAttempt> RecordAttemptAsync(
        string token, int maxAttempts, TimeSpan ttl, CancellationToken ct = default);
}
