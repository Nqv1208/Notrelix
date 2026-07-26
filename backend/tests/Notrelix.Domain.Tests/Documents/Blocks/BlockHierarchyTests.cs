using FluentAssertions;
using Notrelix.Domain.Documents.Blocks;
using Notrelix.Domain.Documents.Rules;

namespace Notrelix.Domain.Tests.Documents.Blocks;

public class BlockHierarchyTests
{
    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _pageId = Guid.NewGuid();
    private readonly Guid _actorId = Guid.NewGuid();
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    [Fact]
    public void MoveToRoot_ShouldSetParentNull()
    {
        var block = Block.Create(_accountId, _workspaceId, _pageId, BlockType.Text,
            BlockContent.Create(JsonValue.EmptyObject()), FractionalIndex.Create("a0"), _actorId, _now);
        block.MoveUnder(
            BlockAncestorPath.Create(_accountId, _workspaceId, _pageId, Guid.NewGuid(), new[] { Guid.NewGuid() }),
            FractionalIndex.Create("a1"), _actorId, _now);
        ((IHasDomainEvents)block).ClearDomainEvents();

        block.MoveToRoot(FractionalIndex.Create("a2"), _actorId, _now);

        block.ParentId.Should().BeNull();
        block.Position.Value.Should().Be("a2");
    }

    [Fact]
    public void MoveToRoot_WhenAlreadyRoot_ShouldBeNoOp()
    {
        var block = Block.Create(_accountId, _workspaceId, _pageId, BlockType.Text,
            BlockContent.Create(JsonValue.EmptyObject()), FractionalIndex.Create("a0"), _actorId, _now);
        ((IHasDomainEvents)block).ClearDomainEvents();

        block.MoveToRoot(FractionalIndex.Create("a0"), _actorId, _now);

        block.ParentId.Should().BeNull();
        block.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void MoveUnder_ShouldSetParent()
    {
        var parentId = Guid.NewGuid();
        var block = Block.Create(_accountId, _workspaceId, _pageId, BlockType.Text,
            BlockContent.Create(JsonValue.EmptyObject()), FractionalIndex.Create("a0"), _actorId, _now);

        block.MoveUnder(
            BlockAncestorPath.Create(_accountId, _workspaceId, _pageId, parentId, new[] { Guid.NewGuid() }),
            FractionalIndex.Create("a1"), _actorId, _now);

        block.ParentId.Should().Be(parentId);
    }

    [Fact]
    public void MoveUnder_ShouldThrow_WhenWouldCreateCycle()
    {
        var blockId = Guid.NewGuid();
        var childId = Guid.NewGuid();

        var block = Block.Create(_accountId, _workspaceId, _pageId, BlockType.Text,
            BlockContent.Create(JsonValue.EmptyObject()), FractionalIndex.Create("a0"), _actorId, _now,
            parentId: Guid.NewGuid());

        var act = () => BlockTreeRules.EnsureNoCycle(blockId,
            BlockAncestorPath.Create(_accountId, _workspaceId, _pageId, childId, new[] { blockId }));

        act.Should().Throw<BusinessRuleException>().WithMessage("*cycle*");
    }

    [Fact]
    public void MoveUnder_ShouldThrow_WhenTargetParentIsSelf()
    {
        var blockId = Guid.NewGuid();

        var act = () => BlockTreeRules.EnsureNoCycle(blockId,
            BlockAncestorPath.Create(_accountId, _workspaceId, _pageId, blockId, new[] { Guid.NewGuid() }));

        act.Should().Throw<BusinessRuleException>().WithMessage("*own parent*");
    }

    [Fact]
    public void MoveUnder_ShouldThrow_WhenDeleted()
    {
        var block = Block.Create(_accountId, _workspaceId, _pageId, BlockType.Text,
            BlockContent.Create(JsonValue.EmptyObject()), FractionalIndex.Create("a0"), _actorId, _now);
        block.SoftDelete(_actorId, _now);

        var act = () => block.MoveUnder(
            BlockAncestorPath.Create(_accountId, _workspaceId, _pageId, Guid.NewGuid(), new[] { Guid.NewGuid() }),
            FractionalIndex.Create("a1"), _actorId, _now);

        act.Should().Throw<DomainException>().WithMessage("*deleted*");
    }

    [Fact]
    public void MoveToRoot_ShouldRaiseEvent()
    {
        var block = Block.Create(_accountId, _workspaceId, _pageId, BlockType.Text,
            BlockContent.Create(JsonValue.EmptyObject()), FractionalIndex.Create("a0"), _actorId, _now);
        block.MoveUnder(
            BlockAncestorPath.Create(_accountId, _workspaceId, _pageId, Guid.NewGuid(), new[] { Guid.NewGuid() }),
            FractionalIndex.Create("a1"), _actorId, _now);
        ((IHasDomainEvents)block).ClearDomainEvents();

        block.MoveToRoot(FractionalIndex.Create("a2"), _actorId, _now);

        block.DomainEvents.Should().ContainSingle(e => e is BlockMovedDomainEvent);
    }

    [Fact]
    public void MoveUnder_ShouldRaiseEvent()
    {
        var block = Block.Create(_accountId, _workspaceId, _pageId, BlockType.Text,
            BlockContent.Create(JsonValue.EmptyObject()), FractionalIndex.Create("a0"), _actorId, _now);
        ((IHasDomainEvents)block).ClearDomainEvents();

        block.MoveUnder(
            BlockAncestorPath.Create(_accountId, _workspaceId, _pageId, Guid.NewGuid(), new[] { Guid.NewGuid() }),
            FractionalIndex.Create("a1"), _actorId, _now);

        block.DomainEvents.Should().ContainSingle(e => e is BlockMovedDomainEvent);
    }
}