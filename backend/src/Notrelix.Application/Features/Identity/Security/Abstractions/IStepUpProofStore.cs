using Notrelix.Application.Features.Identity.Security.DTOs;

namespace Notrelix.Application.Features.Identity.Security.Abstractions;

/// <summary>
/// Single-use storage for VERIFIED step-up proofs. Tokens are consumed exactly
/// once (atomically); only the SHA-256 of the token is used as the cache key.
/// Semantically distinct from IMfaChallengeStore: only a verified proof may
/// authorize a sensitive mutation.
/// </summary>
public interface IStepUpProofStore
{
    Task StoreAsync(string token, StepUpProofPayload payload, TimeSpan ttl, CancellationToken ct = default);

    Task<StepUpProofPayload?> ConsumeAsync(string token, CancellationToken ct = default);
}
