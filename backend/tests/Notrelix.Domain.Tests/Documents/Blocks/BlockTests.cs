using FluentAssertions;
using Notrelix.Domain.Tests.Freeze;
using Notrelix.Domain.Documents.Blocks;

namespace Notrelix.Domain.Tests.Documents;

[CoversAggregate(typeof(Block))]
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

        var block = Block.CreateRoot(Guid.NewGuid(), workspaceId, pageId, BlockType.Text, content, position, createdBy, DateTimeOffset.UtcNow);

        block.WorkspaceId.Should().Be(workspaceId);
        block.PageId.Should().Be(pageId);
        block.Type.Should().Be(BlockType.Text);
        block.Content.Should().Be(content);
        block.DomainEvents.Should().ContainSingle(e => e is BlockCreatedDomainEvent);
    }

    [CoversMutation(typeof(Block), "UpdateProperties(Notrelix.Domain.Documents.Blocks.BlockProperties,System.Guid,System.DateTimeOffset)", MutationScenario.Valid)]
    [Fact]
    public void Create_ShouldApplyDefaultProperties_WhenNoneProvided()
    {
        var block = Block.CreateRoot(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), BlockType.Text, BlockContent.Create(JsonValue.EmptyObject()), FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);

        block.Properties.Should().NotBeNull();
    }

    [CoversMutation(typeof(Block), "UpdateProperties(Notrelix.Domain.Documents.Blocks.BlockProperties,System.Guid,System.DateTimeOffset)", MutationScenario.Event)]
    [CoversMutation(typeof(Block), "UpdateContent(Notrelix.Domain.Documents.Blocks.BlockContent,System.Guid,System.DateTimeOffset)", MutationScenario.Event)]
    [Fact]
    public void UpdateContent_ShouldUpdate_AndRaiseEvent()
    {
        var workspaceId = Guid.NewGuid();
        var block = Block.CreateRoot(Guid.NewGuid(), workspaceId, Guid.NewGuid(), BlockType.Text, BlockContent.Create(JsonValue.EmptyObject()), FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)block).ClearDomainEvents();

        var newContent = BlockContent.Create(JsonValue.Create("{\"text\":\"New\"}"));
        var updatedBy = Guid.NewGuid();

        block.UpdateContent(newContent, updatedBy, DateTimeOffset.UtcNow);

        block.Content.Should().Be(newContent);
        block.UpdatedBy.Should().Be(updatedBy);
        block.DomainEvents.Should().ContainSingle(e => e is BlockContentUpdatedDomainEvent);
    }

    [CoversMutation(typeof(Block), "UpdateProperties(Notrelix.Domain.Documents.Blocks.BlockProperties,System.Guid,System.DateTimeOffset)", MutationScenario.NoOp)]
    [CoversMutation(typeof(Block), "UpdateContent(Notrelix.Domain.Documents.Blocks.BlockContent,System.Guid,System.DateTimeOffset)", MutationScenario.NoOp)]
    [Fact]
    public void UpdateContent_WhenSameContent_ShouldBeNoOp()
    {
        var content = BlockContent.Create(JsonValue.EmptyObject());
        var block = Block.CreateRoot(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), BlockType.Text, content, FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)block).ClearDomainEvents();

        block.UpdateContent(content, Guid.NewGuid(), DateTimeOffset.UtcNow);

        block.DomainEvents.Should().BeEmpty();
    }

    [CoversMutation(typeof(Block), "SoftDelete(System.Guid,System.DateTimeOffset,System.String)", MutationScenario.Lifecycle)]
    [CoversMutation(typeof(Block), "UpdateProperties(Notrelix.Domain.Documents.Blocks.BlockProperties,System.Guid,System.DateTimeOffset)", MutationScenario.Invalid)]
    [CoversMutation(typeof(Block), "UpdateContent(Notrelix.Domain.Documents.Blocks.BlockContent,System.Guid,System.DateTimeOffset)", MutationScenario.Invalid)]
    [Fact]
    public void UpdateContent_WhenDeleted_ShouldThrow()
    {
        var block = Block.CreateRoot(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), BlockType.Text, BlockContent.Create(JsonValue.EmptyObject()), FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        block.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => block.UpdateContent(BlockContent.Create(JsonValue.Create("{\"text\":\"X\"}")), Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<DomainException>().WithMessage("*deleted*");
    }

    [CoversMutation(typeof(Block), "UpdateProperties(Notrelix.Domain.Documents.Blocks.BlockProperties,System.Guid,System.DateTimeOffset)", MutationScenario.Event)]
    [CoversMutation(typeof(Block), "UpdateContent(Notrelix.Domain.Documents.Blocks.BlockContent,System.Guid,System.DateTimeOffset)", MutationScenario.Event)]
    [Fact]
    public void UpdateProperties_ShouldUpdate_AndRaiseEvent()
    {
        var block = Block.CreateRoot(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), BlockType.Text, BlockContent.Create(JsonValue.EmptyObject()), FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)block).ClearDomainEvents();

        var newProps = BlockProperties.Create(JsonValue.Create("{\"color\":\"red\"}"));

        block.UpdateProperties(newProps, Guid.NewGuid(), DateTimeOffset.UtcNow);

        block.Properties.Should().Be(newProps);
        block.DomainEvents.Should().ContainSingle(e => e is BlockPropertiesUpdatedDomainEvent);
    }

    [CoversMutation(typeof(Block), "UpdateProperties(Notrelix.Domain.Documents.Blocks.BlockProperties,System.Guid,System.DateTimeOffset)", MutationScenario.NoOp)]
    [CoversMutation(typeof(Block), "UpdateContent(Notrelix.Domain.Documents.Blocks.BlockContent,System.Guid,System.DateTimeOffset)", MutationScenario.NoOp)]
    [Fact]
    public void UpdateProperties_WhenSame_ShouldBeNoOp()
    {
        var block = Block.CreateRoot(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), BlockType.Text, BlockContent.Create(JsonValue.EmptyObject()), FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        var existingProps = block.Properties;
        ((IHasDomainEvents)block).ClearDomainEvents();

        block.UpdateProperties(existingProps, Guid.NewGuid(), DateTimeOffset.UtcNow);

        block.DomainEvents.Should().BeEmpty();
    }

    [CoversMutation(typeof(Block), "SoftDelete(System.Guid,System.DateTimeOffset,System.String)", MutationScenario.Lifecycle)]
    [CoversMutation(typeof(Block), "UpdateProperties(Notrelix.Domain.Documents.Blocks.BlockProperties,System.Guid,System.DateTimeOffset)", MutationScenario.Invalid)]
    [CoversMutation(typeof(Block), "UpdateContent(Notrelix.Domain.Documents.Blocks.BlockContent,System.Guid,System.DateTimeOffset)", MutationScenario.Invalid)]
    [Fact]
    public void UpdateProperties_WhenDeleted_ShouldThrow()
    {
        var block = Block.CreateRoot(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), BlockType.Text, BlockContent.Create(JsonValue.EmptyObject()), FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        block.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => block.UpdateProperties(BlockProperties.Empty(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<DomainException>().WithMessage("*deleted*");
    }

    [Fact]
    public void MoveToRoot_ShouldSucceed_AndRaiseEvent()
    {
        var block = Block.CreateRoot(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), BlockType.Text, BlockContent.Create(JsonValue.EmptyObject()), FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        block.MoveUnder(
            BlockAncestorPath.Create(block.AccountId, block.WorkspaceId, block.PageId, Guid.NewGuid(), new[] { Guid.NewGuid() }),
            FractionalIndex.Create("a1"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)block).ClearDomainEvents();

        var newPosition = FractionalIndex.Create("a2");

        block.MoveToRoot(newPosition, Guid.NewGuid(), DateTimeOffset.UtcNow);

        block.ParentId.Should().BeNull();
        block.Position.Should().Be(newPosition);
        block.DomainEvents.Should().ContainSingle(e => e is BlockMovedDomainEvent);
    }

    [Fact]
    public void MoveToRoot_WhenSamePositionAndParent_ShouldBeNoOp()
    {
        var block = Block.CreateRoot(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), BlockType.Text, BlockContent.Create(JsonValue.EmptyObject()), FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)block).ClearDomainEvents();

        block.MoveToRoot(FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);

        block.DomainEvents.Should().BeEmpty();
    }

    [CoversMutation(typeof(Block), "SoftDelete(System.Guid,System.DateTimeOffset,System.String)", MutationScenario.Lifecycle)]
    [Fact]
    public void MoveToRoot_WhenDeleted_ShouldThrow()
    {
        var block = Block.CreateRoot(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), BlockType.Text, BlockContent.Create(JsonValue.EmptyObject()), FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        block.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => block.MoveToRoot(FractionalIndex.Create("a1"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<DomainException>().WithMessage("*deleted*");
    }

    [CoversMutation(typeof(Block), "SoftDelete(System.Guid,System.DateTimeOffset,System.String)", MutationScenario.Lifecycle)]
    [Fact]
    public void SoftDelete_ShouldSucceed_AndRaiseEvent()
    {
        var block = Block.CreateRoot(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), BlockType.Text, BlockContent.Create(JsonValue.EmptyObject()), FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)block).ClearDomainEvents();

        block.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        block.IsDeleted.Should().BeTrue();
        block.DomainEvents.Should().ContainSingle(e => e is BlockSoftDeletedDomainEvent);
    }

    [CoversMutation(typeof(Block), "SoftDelete(System.Guid,System.DateTimeOffset,System.String)", MutationScenario.Lifecycle)]
    [Fact]
    public void SoftDelete_WhenAlreadyDeleted_ShouldBeNoOp()
    {
        var block = Block.CreateRoot(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), BlockType.Text, BlockContent.Create(JsonValue.EmptyObject()), FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        block.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)block).ClearDomainEvents();

        block.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        block.DomainEvents.Should().BeEmpty();
    }

    [CoversMutation(typeof(Block), "Restore(System.Guid,System.DateTimeOffset)", MutationScenario.Lifecycle)]
    [Fact]
    public void Restore_ShouldSucceed_AndRaiseEvent()
    {
        var block = Block.CreateRoot(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), BlockType.Text, BlockContent.Create(JsonValue.EmptyObject()), FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        block.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)block).ClearDomainEvents();

        block.Restore(Guid.NewGuid(), DateTimeOffset.UtcNow);

        block.IsDeleted.Should().BeFalse();
        block.DomainEvents.Should().ContainSingle(e => e is BlockRestoredDomainEvent);
    }

    [CoversMutation(typeof(Block), "Restore(System.Guid,System.DateTimeOffset)", MutationScenario.Lifecycle)]
    [CoversMutation(typeof(Block), "SoftDelete(System.Guid,System.DateTimeOffset,System.String)", MutationScenario.Lifecycle)]
    [Fact]
    public void Restore_WhenNotDeleted_ShouldBeNoOp()
    {
        var block = Block.CreateRoot(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), BlockType.Text, BlockContent.Create(JsonValue.EmptyObject()), FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)block).ClearDomainEvents();

        block.Restore(Guid.NewGuid(), DateTimeOffset.UtcNow);

        block.DomainEvents.Should().BeEmpty();
    }
}
