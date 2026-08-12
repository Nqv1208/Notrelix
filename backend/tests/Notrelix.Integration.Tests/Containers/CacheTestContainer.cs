using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using StackExchange.Redis;
using Notrelix.Infrastructure.Data;

namespace Notrelix.Integration.Tests.Containers;

[CollectionDefinition("Cache")]
public class CacheCollection : ICollectionFixture<CacheTestContainer>
{
}

/// <summary>
/// Combined PostgreSQL + Redis fixture for the cache provider certification
/// (FZ-INF-04): the authorized cache behaviors need both the permission version
/// provider (PostgreSQL) and a real Redis server.
/// </summary>
public sealed class CacheTestContainer : IAsyncLifetime
{
    private readonly PostgresTestContainer _postgres = new();
    private IContainer? _redis;

    public string PostgresConnectionString => _postgres.ConnectionString;
    public string RedisConnectionString { get; private set; } = string.Empty;

    public ApplicationDbContext CreatePostgresContext(ICurrentTenantContext? tenant = null) =>
        _postgres.CreateContext(tenant);

    /// <summary>
    /// Restores a pristine state between tests: truncates every table and flushes
    /// Redis, so no test can observe another test's seeded rows or cached values.
    /// </summary>
    public async Task ResetAsync()
    {
        await new DatabaseReset(PostgresConnectionString).ResetAsync();

        using var redis = await ConnectionMultiplexer.ConnectAsync(RedisConnectionString);
        await redis.GetDatabase().ExecuteAsync("FLUSHDB");
    }

    public async Task InitializeAsync()
    {
        await _postgres.InitializeAsync();

        _redis = new ContainerBuilder()
            .WithImage("redis:7-alpine")
            .WithName($"notrelix-test-redis-{Guid.NewGuid():N}")
            .WithPortBinding(6379, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(6379))
            .Build();

        await _redis.StartAsync();
        RedisConnectionString = $"localhost:{_redis.GetMappedPublicPort(6379)}";
    }

    public async Task DisposeAsync()
    {
        if (_redis is not null)
        {
            await _redis.DisposeAsync();
        }

        await _postgres.DisposeAsync();
    }
}
