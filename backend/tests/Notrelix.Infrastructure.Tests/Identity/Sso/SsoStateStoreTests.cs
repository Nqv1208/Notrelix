using Notrelix.Application.Common.Caching;
using Notrelix.Application.Features.Identity.Sso;
using Notrelix.Infrastructure.Identity.Sso;

namespace Notrelix.Infrastructure.Tests.Identity.Sso;

public class SsoStateStoreTests
{
    private sealed class DictionaryCache : IRedisCacheService
    {
        public Dictionary<string, string> Values { get; } = [];
        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
        {
            Values[key] = (string)(object)value!;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<long> IncrementAsync(string key, TimeSpan? expiration = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<T?> GetDeleteAsync<T>(string key, CancellationToken cancellationToken = default) =>
            Task.FromResult(Values.Remove(key, out var value) ? (T?)(object)value : default);

        public Task<(long Attempts, bool Exceeded)> IncrementWithConditionalDeleteAsync(
            string incrementKey, string deleteKey, long max, TimeSpan? ttl = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    [Fact]
    public async Task AStateIsStoredUnderItsDigest_AndWorksOnce()
    {
        var cache = new DictionaryCache();
        var store = new SsoStateStore(cache);
        var state = new SsoLoginState("state-raw-value", Guid.NewGuid(), "a@acme.com", "nonce", "verifier", "https://api/cb", "/home", "https://login.acme.example", "binding-hash", DateTimeOffset.UtcNow.AddMinutes(10));

        await store.StoreAsync(state, TimeSpan.FromMinutes(10), CancellationToken.None);

        cache.Values.Keys.Should().ContainSingle().Which.Should().StartWith("Notrelix_sso_state:").And.NotContain("state-raw-value");
        (await store.ConsumeAsync("state-raw-value", CancellationToken.None)).Should().Be(state);
        (await store.ConsumeAsync("state-raw-value", CancellationToken.None)).Should().BeNull("a state works once");
        (await store.ConsumeAsync("another", CancellationToken.None)).Should().BeNull();
    }
}
