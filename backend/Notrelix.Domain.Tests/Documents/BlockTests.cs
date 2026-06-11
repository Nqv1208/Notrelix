using FluentAssertions;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.Documents.Blocks;
using Notrelix.Domain.Documents.Blocks;
using Notrelix.Domain.SharedKernel;
using Xunit;

namespace Notrelix.Domain.Tests.Documents;

public class BlockTests
{
    [Fact]
    public void Create_ShouldSucceed_AndRaiseEvent()
    {
        var pageId = Guid.NewGuid();
        var content = BlockContent.Create(JsonValue.Create("{\"text\":\"Hello\"}"));
        var position = FractionalIndex.Create(1.0);
        var createdBy = Guid.NewGuid();

        var block = Block.Create(pageId, BlockType.Text, content, position, createdBy);

        block.PageId.Should().Be(pageId);
        block.Type.Should().Be(BlockType.Text);
        block.Content.Should().Be(content);
        block.DomainEvents.Should().ContainSingle(e => e is BlockCreatedEvent);
    }

    [Fact]
    public void UpdateContent_ShouldUpdate_AndRaiseEvent()
    {
        var block = Block.Create(Guid.NewGuid(), BlockType.Text, BlockContent.Create(JsonValue.EmptyObject()), FractionalIndex.Create(1), Guid.NewGuid());
        block.ClearDomainEvents();

        var newContent = BlockContent.Create(JsonValue.Create("{\"text\":\"New\"}"));
        var updatedBy = Guid.NewGuid();
        
        block.UpdateContent(newContent, updatedBy);

        block.Content.Should().Be(newContent);
        block.UpdatedBy.Should().Be(updatedBy);
        block.DomainEvents.Should().ContainSingle(e => e is BlockUpdatedEvent);
    }
}
