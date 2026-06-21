using Notrelix.Infrastructure.Data.Projections.Collab;

namespace Notrelix.Infrastructure.Tests.Data.Projections;

public class UnreadCounterRecordTests
{
    [Fact]
    public void Create_sets_properties()
    {
        var id = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var counter = UnreadCounterRecord.Create(
            id, workspaceId, userId, "Comment", 1, now);

        counter.Id.Should().Be(id);
        counter.WorkspaceId.Should().Be(workspaceId);
        counter.UserId.Should().Be(userId);
        counter.CounterType.Should().Be("Comment");
        counter.CounterValue.Should().Be(1);
        counter.UpdatedAt.Should().Be(now);
    }
}
