using Notrelix.Integration.Tests.Containers;
using Notrelix.Infrastructure.Data.Ops.Stores;

namespace Notrelix.Integration.Tests.Data.Ops;

[Collection("Database")]
[Trait("Category", "RequiresDocker")]
public class IdempotencyStoreIntegrationTests
{
    private readonly PostgresTestContainer _db;

    public IdempotencyStoreIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task TryAcquireAsync_WhenKeyAvailable_ReturnsGuid()
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        var store = new IdempotencyStore(conn);
        var key = $"test-key-{Guid.NewGuid():N}";
        var now = DateTimeOffset.UtcNow;

        var acquired = await store.TryAcquireAsync(
            key, "test-scope", "POST", "/api/test", "hash",
            Guid.NewGuid(), Guid.NewGuid(), now.AddMinutes(30), now);

        acquired.Should().NotBeNull();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task TryAcquireAsync_WhenKeyAlreadyAcquired_ReturnsNull()
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        var store = new IdempotencyStore(conn);
        var key = $"test-key-{Guid.NewGuid():N}";
        var now = DateTimeOffset.UtcNow;

        var first = await store.TryAcquireAsync(
            key, "test-scope", "POST", "/api/test", "hash",
            Guid.NewGuid(), Guid.NewGuid(), now.AddMinutes(30), now);
        first.Should().NotBeNull();

        var second = await store.TryAcquireAsync(
            key, "test-scope", "POST", "/api/test", "hash",
            Guid.NewGuid(), Guid.NewGuid(), now.AddMinutes(30), now);
        second.Should().BeNull();
    }
}
