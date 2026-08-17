using System.Text.Json;
using Notrelix.Application.Features.Identity.Security.Abstractions;
using Notrelix.Application.Features.Identity.Security.DTOs;

namespace Notrelix.Infrastructure.Identity.Security;

/// <summary>
/// Redis-backed single-use storage for VERIFIED step-up proofs.
/// Only the SHA-256 of the proof token is used as the cache key;
/// consuming removes the entry atomically so a token can never be replayed.
/// Semantically distinct from MfaChallengeStore.
/// </summary>
public sealed class StepUpProofStore : IStepUpProofStore
{
    private readonly IRedisCacheService _cache;

    private const string KeyPrefix = "identity:security:step-up-proof:";

    public StepUpProofStore(IRedisCacheService cache)
    {
        _cache = cache;
    }

    public async Task StoreAsync(string token, StepUpProofPayload payload, TimeSpan ttl, CancellationToken ct)
    {
        var key = GetKey(token);
        var value = JsonSerializer.Serialize(payload);
        await _cache.SetAsync(key, value, ttl, ct);
    }

    public async Task<StepUpProofPayload?> ConsumeAsync(string token, CancellationToken ct)
    {
        var key = GetKey(token);
        var value = await _cache.GetDeleteAsync<string>(key, ct);
        return value is null ? null : JsonSerializer.Deserialize<StepUpProofPayload>(value);
    }

    private static string GetKey(string token)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        var hashString = Convert.ToHexString(hash).ToLowerInvariant();
        return $"{KeyPrefix}{hashString}";
    }
}
