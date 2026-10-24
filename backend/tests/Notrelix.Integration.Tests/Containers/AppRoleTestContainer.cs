using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Notrelix.Testing.Integration;

namespace Notrelix.Integration.Tests.Containers;

[CollectionDefinition("DatabaseAppRole")]
public class DatabaseAppRoleCollection : ICollectionFixture<AppRoleTestContainer>
{
}

/// <summary>
/// PostgreSQL migrated and carrying the row-level-security policy pack, plus Redis, with two connections:
/// the owner (container superuser, used to seed) and a non-owner login running as <c>notrelix_app</c>
/// (used by the host under test). This is the production-shaped database the conformance suite runs
/// against; the shared fixtures connect as the superuser and therefore never exercise the policies.
/// </summary>
public sealed class AppRoleTestContainer : IAsyncLifetime
{
    private readonly PostgresTestContainer _postgres = new();
    private IContainer? _redis;

    public string OwnerConnectionString => _postgres.OwnerConnectionString;
    public string AppConnectionString { get; private set; } = string.Empty;
    public string RedisConnectionString { get; private set; } = string.Empty;

    public Notrelix.Infrastructure.Data.ApplicationDbContext CreateOwnerContext(ICurrentTenantContext? tenant = null) =>
        _postgres.CreateContext(tenant);

    /// <summary>A context connected as the application-role login (row-level security applies).</summary>
    public Notrelix.Infrastructure.Data.ApplicationDbContext CreateAppContext(ICurrentTenantContext? tenant = null) =>
        PostgresTestContainer.CreateContextFor(AppConnectionString, tenant);

    public async Task InitializeAsync()
    {
        await _postgres.InitializeAsync();
        await _postgres.ApplyRlsAsync();
        AppConnectionString = await AppRoleLogin.CreateAsync(OwnerConnectionString, "notrelix_conformance_login");

        _redis = new ContainerBuilder()
            .WithImage("redis:7-alpine")
            .WithName($"notrelix-test-redis-{Guid.NewGuid():N}")
            .WithPortBinding(6379, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(6379))
            .Build();
        await _redis.StartAsync();
        RedisConnectionString = $"localhost:{_redis.GetMappedPublicPort(6379)}";
    }

    /// <summary>Truncates every table as the owner so each test starts from an empty database.</summary>
    public Task ResetAsync() => new DatabaseReset(OwnerConnectionString).ResetAsync();

    public async Task DisposeAsync()
    {
        if (_redis is not null)
        {
            await _redis.DisposeAsync();
        }

        await _postgres.DisposeAsync();
    }
}
