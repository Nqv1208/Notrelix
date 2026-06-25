using System.Data;
using Notrelix.Infrastructure.Data.Ops.Stores;

namespace Notrelix.Infrastructure.Tests.Data.Ops;

public class JobLockManagerTests
{
    private readonly Mock<IDbConnection> _connectionMock = new();
    private readonly JobLockManager _manager;

    public JobLockManagerTests()
    {
        _manager = new JobLockManager(_connectionMock.Object);
    }

    [Fact]
    public void Constructor_WhenNullConnection_DoesNotThrow()
    {
        var act = () => new JobLockManager(null!);
        act.Should().NotThrow();
    }
}
