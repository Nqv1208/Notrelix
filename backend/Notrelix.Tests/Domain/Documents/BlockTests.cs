using FluentAssertions;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.Documents.Blocks;
using Notrelix.Domain.SharedKernel;
using Xunit;

namespace Notrelix.Domain.Tests.Documents;

public class BlockTests
{
    [Fact]
    public void Create_ShouldSucceed_AndRaiseEvent()
    {
        var workspaceId = Guid.NewGuid();
        var pageId = Guid.NewGuid();
        var content = BlockContent.Create(JsonValue.Create("{\"text\":\"Hello\"}"));
        var position = FractionalIndex.Create("a0");
        var createdBy = Guid.NewGuid();

        var block = Block.Create(workspaceId, pageId, BlockType.Text, content, position, createdBy, DateTimeOffset.UtcNow);

        block.WorkspaceId.Should().Be(workspaceId);
        block.PageId.Should().Be(pageId);
        block.Type.Should().Be(BlockType.Text);
        block.Content.Should().Be(content);
        block.DomainEvents.Should().ContainSingle(e => e is BlockCreatedEvent);
    }

    [Fact]
    public void UpdateContent_ShouldUpdate_AndRaiseEvent()
    {
        var workspaceId = Guid.NewGuid();
        var block = Block.Create(workspaceId, Guid.NewGuid(), BlockType.Text, BlockContent.Create(JsonValue.EmptyObject()), FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        block.ClearDomainEvents();

        var newContent = BlockContent.Create(JsonValue.Create("{\"text\":\"New\"}"));
        var updatedBy = Guid.NewGuid();
        
        block.UpdateContent(newContent, updatedBy, DateTimeOffset.UtcNow);

        block.Content.Should().Be(newContent);
        block.UpdatedBy.Should().Be(updatedBy);
        block.DomainEvents.Should().ContainSingle(e => e is BlockContentUpdatedEvent);
    }
}
