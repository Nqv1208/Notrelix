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

    /// <summary>The application-role login the code under test runs on.</summary>
    public string PostgresConnectionString => _postgres.ConnectionString;

    /// <summary>The worker-role login background work runs on.</summary>
    public string PostgresWorkerConnectionString => _postgres.WorkerConnectionString;

    /// <summary>The container superuser, for seeding and verification only.</summary>
    public string PostgresOwnerConnectionString => _postgres.OwnerConnectionString;
    public string RedisConnectionString { get; private set; } = string.Empty;

    /// <summary>A context on the owner connection, for seeding and verification only.</summary>
    public ApplicationDbContext CreatePostgresOwnerContext(ICurrentTenantContext? tenant = null) =>
        _postgres.CreateOwnerContext(tenant);

    /// <summary>
    /// Restores a pristine state between tests: truncates every table and flushes
    /// Redis, so no test can observe another test's seeded rows or cached values.
    /// </summary>
    public async Task ResetAsync()
    {
        await new DatabaseReset(PostgresOwnerConnectionString).ResetAsync();

        var options = new ConfigurationOptions
        {
            EndPoints = { RedisConnectionString },
            AllowAdmin = true, // FLUSHDB requires admin-mode commands.
            ConnectRetry = 3,
            ConnectTimeout = 10_000,
        };
        using var redis = await ConnectionMultiplexer.ConnectAsync(options);
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
