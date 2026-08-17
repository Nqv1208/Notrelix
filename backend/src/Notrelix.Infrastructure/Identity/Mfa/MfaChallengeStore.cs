using System.Text.Json;
using Notrelix.Application.Features.Identity.Mfa.Abstractions;
using Notrelix.Application.Features.Identity.Mfa.DTOs;

namespace Notrelix.Infrastructure.Identity.Mfa;

/// <summary>
/// Redis-backed single-use MFA challenge storage.
/// Only the SHA-256 of the challenge token is used as the cache key;
/// consuming removes the entry atomically so a token can never be replayed.
/// </summary>
public sealed class MfaChallengeStore : IMfaChallengeStore
{
    private readonly IRedisCacheService _cache;

    private const string KeyPrefix = "identity:mfa:challenge:";

    public MfaChallengeStore(IRedisCacheService cache)
    {
        _cache = cache;
    }

    public async Task StoreAsync(string token, MfaChallengePayload payload, TimeSpan ttl, CancellationToken ct)
    {
        var key = GetKey(token);
        var value = JsonSerializer.Serialize(payload);
        await _cache.SetAsync(key, value, ttl, ct);
    }

    public async Task<MfaChallengePayload?> PeekAsync(string token, CancellationToken ct)
    {
        var key = GetKey(token);
        var value = await _cache.GetAsync<string>(key, ct);
        return value is null ? null : JsonSerializer.Deserialize<MfaChallengePayload>(value);
    }

    public async Task<MfaChallengePayload?> ConsumeAsync(string token, CancellationToken ct)
    {
        var key = GetKey(token);
        var value = await _cache.GetDeleteAsync<string>(key, ct);
        return value is null ? null : JsonSerializer.Deserialize<MfaChallengePayload>(value);
    }

    private static string GetKey(string token)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        var hashString = Convert.ToHexString(hash).ToLowerInvariant();
        return $"{KeyPrefix}{hashString}";
    }
}
