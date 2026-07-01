using FluentAssertions;
using Notrelix.Domain.Documents.Blocks;

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

        var block = Block.Create(Guid.NewGuid(), workspaceId, pageId, BlockType.Text, content, position, createdBy, DateTimeOffset.UtcNow);

        block.WorkspaceId.Should().Be(workspaceId);
        block.PageId.Should().Be(pageId);
        block.Type.Should().Be(BlockType.Text);
        block.Content.Should().Be(content);
        block.DomainEvents.Should().ContainSingle(e => e is BlockCreatedDomainEvent);
    }

    [Fact]
    public void Create_ShouldApplyDefaultProperties_WhenNoneProvided()
    {
        var block = Block.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), BlockType.Text, BlockContent.Create(JsonValue.EmptyObject()), FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);

        block.Properties.Should().NotBeNull();
    }

    [Fact]
    public void UpdateContent_ShouldUpdate_AndRaiseEvent()
    {
        var workspaceId = Guid.NewGuid();
        var block = Block.Create(Guid.NewGuid(), workspaceId, Guid.NewGuid(), BlockType.Text, BlockContent.Create(JsonValue.EmptyObject()), FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        block.ClearDomainEvents();

        var newContent = BlockContent.Create(JsonValue.Create("{\"text\":\"New\"}"));
        var updatedBy = Guid.NewGuid();

        block.UpdateContent(newContent, updatedBy, DateTimeOffset.UtcNow);

        block.Content.Should().Be(newContent);
        block.UpdatedBy.Should().Be(updatedBy);
        block.DomainEvents.Should().ContainSingle(e => e is BlockContentUpdatedDomainEvent);
    }

    [Fact]
    public void UpdateContent_WhenSameContent_ShouldBeNoOp()
    {
        var content = BlockContent.Create(JsonValue.EmptyObject());
        var block = Block.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), BlockType.Text, content, FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        block.ClearDomainEvents();

        block.UpdateContent(content, Guid.NewGuid(), DateTimeOffset.UtcNow);

        block.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void UpdateContent_WhenDeleted_ShouldThrow()
    {
        var block = Block.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), BlockType.Text, BlockContent.Create(JsonValue.EmptyObject()), FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        block.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => block.UpdateContent(BlockContent.Create(JsonValue.Create("{\"text\":\"X\"}")), Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<DomainException>().WithMessage("*deleted*");
    }

    [Fact]
    public void UpdateProperties_ShouldUpdate_AndRaiseEvent()
    {
        var block = Block.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), BlockType.Text, BlockContent.Create(JsonValue.EmptyObject()), FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        block.ClearDomainEvents();

        var newProps = BlockProperties.Create(JsonValue.Create("{\"color\":\"red\"}"));

        block.UpdateProperties(newProps, Guid.NewGuid(), DateTimeOffset.UtcNow);

        block.Properties.Should().Be(newProps);
        block.DomainEvents.Should().ContainSingle(e => e is BlockPropertiesUpdatedDomainEvent);
    }

    [Fact]
    public void UpdateProperties_WhenSame_ShouldBeNoOp()
    {
        var block = Block.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), BlockType.Text, BlockContent.Create(JsonValue.EmptyObject()), FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        var existingProps = block.Properties;
        block.ClearDomainEvents();

        block.UpdateProperties(existingProps, Guid.NewGuid(), DateTimeOffset.UtcNow);

        block.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void UpdateProperties_WhenDeleted_ShouldThrow()
    {
        var block = Block.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), BlockType.Text, BlockContent.Create(JsonValue.EmptyObject()), FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        block.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => block.UpdateProperties(BlockProperties.Empty(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<DomainException>().WithMessage("*deleted*");
    }

    [Fact]
    public void Move_ShouldSucceed_AndRaiseEvent()
    {
        var block = Block.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), BlockType.Text, BlockContent.Create(JsonValue.EmptyObject()), FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        block.ClearDomainEvents();

        var newPosition = FractionalIndex.Create("b0");

        block.Move(null, newPosition, Guid.NewGuid(), DateTimeOffset.UtcNow);

        block.Position.Should().Be(newPosition);
        block.DomainEvents.Should().ContainSingle(e => e is BlockMovedDomainEvent);
    }

    [Fact]
    public void Move_WhenSamePositionAndParent_ShouldBeNoOp()
    {
        var block = Block.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), BlockType.Text, BlockContent.Create(JsonValue.EmptyObject()), FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        block.ClearDomainEvents();

        block.Move(null, FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);

        block.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Move_WhenDeleted_ShouldThrow()
    {
        var block = Block.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), BlockType.Text, BlockContent.Create(JsonValue.EmptyObject()), FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        block.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => block.Move(null, FractionalIndex.Create("b0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<DomainException>().WithMessage("*deleted*");
    }

    [Fact]
    public void SoftDelete_ShouldSucceed_AndRaiseEvent()
    {
        var block = Block.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), BlockType.Text, BlockContent.Create(JsonValue.EmptyObject()), FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        block.ClearDomainEvents();

        block.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        block.IsDeleted.Should().BeTrue();
        block.DomainEvents.Should().ContainSingle(e => e is BlockSoftDeletedDomainEvent);
    }

    [Fact]
    public void SoftDelete_WhenAlreadyDeleted_ShouldBeNoOp()
    {
        var block = Block.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), BlockType.Text, BlockContent.Create(JsonValue.EmptyObject()), FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        block.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);
        block.ClearDomainEvents();

        block.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        block.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Restore_ShouldSucceed_AndRaiseEvent()
    {
        var block = Block.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), BlockType.Text, BlockContent.Create(JsonValue.EmptyObject()), FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        block.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);
        block.ClearDomainEvents();

        block.Restore(Guid.NewGuid(), DateTimeOffset.UtcNow);

        block.IsDeleted.Should().BeFalse();
        block.DomainEvents.Should().ContainSingle(e => e is BlockRestoredDomainEvent);
    }

    [Fact]
    public void Restore_WhenNotDeleted_ShouldBeNoOp()
    {
        var block = Block.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), BlockType.Text, BlockContent.Create(JsonValue.EmptyObject()), FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        block.ClearDomainEvents();

        block.Restore(Guid.NewGuid(), DateTimeOffset.UtcNow);

        block.DomainEvents.Should().BeEmpty();
    }
}
