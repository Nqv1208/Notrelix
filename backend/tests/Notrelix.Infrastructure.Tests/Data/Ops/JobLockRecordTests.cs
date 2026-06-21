using Notrelix.Infrastructure.Data.Ops.Entities;

namespace Notrelix.Infrastructure.Tests.Data.Ops;

public class JobLockRecordTests
{
    [Fact]
    public void Create_sets_fencing_token()
    {
        var now = DateTimeOffset.UtcNow;
        var lock_ = JobLockRecord.Create(
            Guid.NewGuid(), "my-lock", "worker-1", 1,
            now.AddMinutes(5), "{}", now, now);

        lock_.LockKey.Should().Be("my-lock");
        lock_.LockedBy.Should().Be("worker-1");
        lock_.FencingToken.Should().Be(1);
        lock_.RenewedAt.Should().BeNull();
    }
}
