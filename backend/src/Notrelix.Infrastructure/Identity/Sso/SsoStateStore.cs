using System.Text.Json;
using Notrelix.Application.Features.Identity.Sso;

namespace Notrelix.Infrastructure.Identity.Sso;

public sealed class SsoStateStore : ISsoStateStore
{
    private const string KeyPrefix = "Notrelix_sso_state:";

    private readonly IRedisCacheService _cache;

    public SsoStateStore(IRedisCacheService cache) => _cache = cache;

    public Task StoreAsync(SsoLoginState state, TimeSpan ttl, CancellationToken cancellationToken) =>
        _cache.SetAsync(KeyOf(state.State), JsonSerializer.Serialize(state), ttl);

    public async Task<SsoLoginState?> ConsumeAsync(string state, CancellationToken cancellationToken)
    {
        // One atomic read-and-remove: two callbacks racing with the same state cannot both get it.
        var value = await _cache.GetDeleteAsync<string>(KeyOf(state), cancellationToken);
        return value is null ? null : JsonSerializer.Deserialize<SsoLoginState>(value);
    }

    private static string KeyOf(string state) =>
        KeyPrefix + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(state))).ToLowerInvariant();
}
