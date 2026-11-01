using Microsoft.Extensions.Caching.StackExchangeRedis;
using Notrelix.Infrastructure.Caching;
using Notrelix.Infrastructure.Identity.Auth;
using Notrelix.Integration.Tests.Containers;
using StackExchange.Redis;

namespace Notrelix.Integration.Tests.Auth;

/// <summary>
/// The refresh rotation ledger on a real Redis (UC-IDN-D01, D03): of many parallel refreshes with one
/// credential exactly one claims the rotation, a released claim can be taken again, and the
/// successor is readable only with the credential it replaced.
/// </summary>
[Collection("Cache")]
[Trait("Category", "Integration")]
public sealed class RefreshRotationLedgerIntegrationTests : IAsyncLifetime
{
    private readonly CacheTestContainer _fixture;
    private ConnectionMultiplexer _redis = null!;
    private RefreshRotationLedger _ledger = null!;

    public RefreshRotationLedgerIntegrationTests(CacheTestContainer fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        var config = ConfigurationOptions.Parse(_fixture.RedisConnectionString);
        config.AbortOnConnectFail = false;
        _redis = ConnectionMultiplexer.Connect(config);
        var cache = new RedisCache(new RedisCacheOptions
        {
            Configuration = _fixture.RedisConnectionString,
            InstanceName = "Notrelix_",
        });
        _ledger = new RefreshRotationLedger(new RedisCacheService(cache, _redis));
    }

    public Task DisposeAsync()
    {
        _redis.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task OfManyParallelRefreshes_WithOneCredential_ExactlyOneClaimsTheRotation()
    {
        var hash = $"hash-{Guid.NewGuid():N}";

        var claims = await Task.WhenAll(Enumerable.Range(0, 25).Select(_ => _ledger.TryClaimAsync(hash, CancellationToken.None)));

        claims.Count(c => c).Should().Be(1);
    }

    [Fact]
    public async Task ADifferentCredential_HasItsOwnClaim()
    {
        (await _ledger.TryClaimAsync($"hash-{Guid.NewGuid():N}", CancellationToken.None)).Should().BeTrue();
        (await _ledger.TryClaimAsync($"hash-{Guid.NewGuid():N}", CancellationToken.None)).Should().BeTrue();
    }

    [Fact]
    public async Task AReleasedClaim_CanBeTakenByTheRetry_AndNotBeforeTheRelease()
    {
        var hash = $"hash-{Guid.NewGuid():N}";

        (await _ledger.TryClaimAsync(hash, CancellationToken.None)).Should().BeTrue();
        (await _ledger.TryClaimAsync(hash, CancellationToken.None)).Should().BeFalse();
        await _ledger.ReleaseClaimAsync(hash, CancellationToken.None);
        (await _ledger.TryClaimAsync(hash, CancellationToken.None)).Should().BeTrue();
    }

    [Fact]
    public async Task TheSuccessor_IsReadBackOnlyWithTheCredentialItReplaced_AndRedisHoldsNoLiveCredential()
    {
        var replaced = $"old-{Guid.NewGuid():N}";
        var successor = $"new-{Guid.NewGuid():N}";

        await _ledger.RecordSuccessorAsync(replaced, successor, CancellationToken.None);

        (await _ledger.RecallSuccessorAsync(replaced, CancellationToken.None)).Should().Be(successor);
        (await _ledger.RecallSuccessorAsync($"other-{Guid.NewGuid():N}", CancellationToken.None)).Should().BeNull();
        var held = _redis.GetServer(_redis.GetEndPoints()[0]).Keys(pattern: "*refresh:successor:*").ToList();
        held.Should().ContainSingle();
        // The distributed cache keeps a hash; none of its fields may carry the credential in clear.
        foreach (var field in await _redis.GetDatabase().HashGetAllAsync(held[0]))
            System.Text.Encoding.UTF8.GetString((byte[])field.Value!).Should().NotContain(successor);
    }

    [Fact]
    public async Task ACredentialThatWasNeverReplaced_RecallsNothing()
    {
        (await _ledger.RecallSuccessorAsync($"never-{Guid.NewGuid():N}", CancellationToken.None)).Should().BeNull();
    }
}
