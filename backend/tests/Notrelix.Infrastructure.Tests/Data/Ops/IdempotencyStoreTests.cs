using System.Data;
using Notrelix.Infrastructure.Data.Ops.Stores;

namespace Notrelix.Infrastructure.Tests.Data.Ops;

public class IdempotencyStoreTests
{
    private readonly Mock<IDbConnection> _connectionMock = new();
    private readonly IdempotencyStore _store;

    public IdempotencyStoreTests()
    {
        _store = new IdempotencyStore(_connectionMock.Object);
    }

    [Fact]
    public void Constructor_WhenNullConnection_DoesNotThrow()
    {
        var act = () => new IdempotencyStore(null!);
        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_WhenValidConnection_Succeeds()
    {
        var store = new IdempotencyStore(_connectionMock.Object);
        store.Should().NotBeNull();
    }
}
