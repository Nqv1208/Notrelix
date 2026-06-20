using System.Data;
using Moq;
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
    public void Constructor_throws_on_null_connection()
    {
        var act = () => new JobLockManager(null!);
        act.Should().Throw<ArgumentNullException>();
    }
}
