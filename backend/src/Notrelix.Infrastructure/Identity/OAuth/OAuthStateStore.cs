using System.Text.Json;
using Notrelix.Application.Features.Identity.OAuth.Abstractions;
using Notrelix.Application.Features.Identity.OAuth.DTOs;

namespace Notrelix.Infrastructure.Identity.OAuth;

public sealed class OAuthStateStore : IOAuthStateStore
{
    private readonly IRedisCacheService _cache;
    private readonly OAuthOptions _options;

    private const string KeyPrefix = "Notrelix_oauth_state:";

    public OAuthStateStore(IRedisCacheService cache, IOptions<OAuthOptions> options)
    {
        _cache = cache;
        _options = options.Value;
    }

    public async Task StoreAsync(OAuthLoginState state, TimeSpan ttl, CancellationToken ct)
    {
        var key = GetKey(state.State);
        var value = JsonSerializer.Serialize(state);
        await _cache.SetAsync(key, value, ttl);
    }

    public async Task<OAuthLoginState?> ConsumeAsync(string state, CancellationToken ct)
    {
        var key = GetKey(state);
        var value = await _cache.GetAsync<string>(key);

        if (value is null)
            return null;

        await _cache.RemoveAsync(key);
        return JsonSerializer.Deserialize<OAuthLoginState>(value);
    }

    private static string GetKey(string state)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(state));
        var hashString = Convert.ToHexString(hash).ToLowerInvariant();
        return $"{KeyPrefix}{hashString}";
    }
}
