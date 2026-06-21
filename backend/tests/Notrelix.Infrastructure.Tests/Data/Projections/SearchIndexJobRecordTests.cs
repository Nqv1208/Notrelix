using Notrelix.Infrastructure.Data.Projections.Search;

namespace Notrelix.Infrastructure.Tests.Data.Projections;

public class SearchIndexJobRecordTests
{
    [Fact]
    public void Create_sets_defaults()
    {
        var id = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var job = SearchIndexJobRecord.Create(
            id, Guid.NewGuid(), "BoardItem", resourceId,
            "Upsert", "Pending", 100, 5, now,
            null, null, "{}", now);

        job.Id.Should().Be(id);
        job.Operation.Should().Be("Upsert");
        job.Status.Should().Be("Pending");
        job.Priority.Should().Be(100);
        job.AttemptCount.Should().Be(0);
        job.MaxAttempts.Should().Be(5);
        job.LockedBy.Should().BeNull();
        job.LockedUntil.Should().BeNull();
        job.ErrorMessage.Should().BeNull();
        job.ProcessedAt.Should().BeNull();
    }

    [Fact]
    public void Create_allows_null_workspace_id()
    {
        var job = SearchIndexJobRecord.Create(
            Guid.NewGuid(), null, "Page", Guid.NewGuid(),
            "Delete", "Pending", 50, 3, DateTimeOffset.UtcNow,
            null, null, "{}", DateTimeOffset.UtcNow);

        job.WorkspaceId.Should().BeNull();
    }
}
