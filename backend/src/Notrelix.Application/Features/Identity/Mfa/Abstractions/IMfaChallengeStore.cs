using Notrelix.Application.Features.Identity.Mfa.DTOs;

namespace Notrelix.Application.Features.Identity.Mfa.Abstractions;

/// <summary>
/// Single-use MFA challenge storage. Tokens are consumed exactly once;
/// only the SHA-256 of the token is used as the cache key.
/// </summary>
public interface IMfaChallengeStore
{
    Task StoreAsync(string token, MfaChallengePayload payload, TimeSpan ttl, CancellationToken ct = default);
    Task<MfaChallengePayload?> ConsumeAsync(string token, CancellationToken ct = default);
}
