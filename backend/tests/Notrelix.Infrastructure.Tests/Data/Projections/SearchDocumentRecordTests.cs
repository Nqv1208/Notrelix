using Notrelix.Infrastructure.Data.Projections.Search;

namespace Notrelix.Infrastructure.Tests.Data.Projections;

public class SearchDocumentRecordTests
{
    [Fact]
    public void Create_sets_all_properties()
    {
        var id = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var record = SearchDocumentRecord.Create(
            id, accountId, workspaceId, "BoardItem", resourceId,
            "Test Title", "Test content",
            ["tag1", "tag2"], "{\"key\":\"value\"}", now);

        record.Id.Should().Be(id);
        record.AccountId.Should().Be(accountId);
        record.WorkspaceId.Should().Be(workspaceId);
        record.ResourceType.Should().Be("BoardItem");
        record.ResourceId.Should().Be(resourceId);
        record.Title.Should().Be("Test Title");
        record.Content.Should().Be("Test content");
        record.Tags.Should().BeEquivalentTo(["tag1", "tag2"]);
        record.MetadataJson.Should().Be("{\"key\":\"value\"}");
        record.CreatedAt.Should().Be(now);
        record.UpdatedAt.Should().BeNull();
        record.SearchVector.Should().BeNull();
    }

    [Fact]
    public void Create_with_null_content_sets_null()
    {
        var record = SearchDocumentRecord.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Page", Guid.NewGuid(),
            "Title", null, [], "{}", DateTimeOffset.UtcNow);

        record.Content.Should().BeNull();
    }

    [Fact]
    public void Create_with_empty_tags_creates_empty_array()
    {
        var record = SearchDocumentRecord.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Page", Guid.NewGuid(),
            "Title", null, [], "{}", DateTimeOffset.UtcNow);

        record.Tags.Should().BeEmpty();
    }
}
