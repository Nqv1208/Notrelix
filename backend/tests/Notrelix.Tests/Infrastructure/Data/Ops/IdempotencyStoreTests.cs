using System.Data;
using Moq;
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
    public void Constructor_throws_on_null_connection()
    {
        var act = () => new IdempotencyStore(null!);
        act.Should().Throw<ArgumentNullException>();
    }
}
