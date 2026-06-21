using Notrelix.Infrastructure.Data.Ops.Entities;

namespace Notrelix.Infrastructure.Tests.Data.Ops;

public class IdempotencyKeyRecordTests
{
    [Fact]
    public void Create_sets_started_status()
    {
        var key = IdempotencyKeyRecord.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "api", "key-123", "POST", "/api/boards", "hash",
            DateTimeOffset.UtcNow.AddHours(1), DateTimeOffset.UtcNow);

        key.Status.Should().Be("Started");
        key.ResponseStatusCode.Should().BeNull();
        key.CompletedAt.Should().BeNull();
    }
}
