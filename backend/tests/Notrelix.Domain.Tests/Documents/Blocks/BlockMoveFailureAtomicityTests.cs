using FluentAssertions;
using Notrelix.Domain.Documents.Blocks;
using Notrelix.Domain.Tests.Freeze;

namespace Notrelix.Domain.Tests.Documents.Blocks;

public class BlockMoveFailureAtomicityTests
{
    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _pageId = Guid.NewGuid();
    private readonly Guid _actorId = Guid.NewGuid();
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    private Block CreateRootBlock() => Block.CreateRoot(_accountId, _workspaceId, _pageId, BlockType.Text,
        BlockContent.Create(JsonValue.EmptyObject()), FractionalIndex.Create("a0"), _actorId, _now);

    [CoversMutation(typeof(Block), nameof(Block.MoveUnder), MutationScenario.FailureAtomicity, typeof(BlockAncestorPath), typeof(FractionalIndex), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void MoveUnder_WithWrongAccount_ShouldNotMutateRoot()
    {
        var block = CreateRootBlock();
        var path = BlockAncestorPath.Create(Guid.NewGuid(), _workspaceId, _pageId, Guid.NewGuid(), [Guid.NewGuid()]);
        var before = block.Version;
        var act = () => block.MoveUnder(path, FractionalIndex.Create("a1"), _actorId, _now);
        act.Should().Throw<BusinessRuleException>();
        block.Version.Should().Be(before);
    }

    [CoversMutation(typeof(Block), nameof(Block.MoveUnder), MutationScenario.FailureAtomicity, typeof(BlockAncestorPath), typeof(FractionalIndex), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void MoveUnder_WithWrongWorkspace_ShouldNotMutateRoot()
    {
        var block = CreateRootBlock();
        var path = BlockAncestorPath.Create(_accountId, Guid.NewGuid(), _pageId, Guid.NewGuid(), [Guid.NewGuid()]);
        var before = block.Version;
        var act = () => block.MoveUnder(path, FractionalIndex.Create("a1"), _actorId, _now);
        act.Should().Throw<BusinessRuleException>();
        block.Version.Should().Be(before);
    }

    [CoversMutation(typeof(Block), nameof(Block.MoveUnder), MutationScenario.FailureAtomicity, typeof(BlockAncestorPath), typeof(FractionalIndex), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void MoveUnder_WithWrongPage_ShouldNotMutateRoot()
    {
        var block = CreateRootBlock();
        var path = BlockAncestorPath.Create(_accountId, _workspaceId, Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()]);
        var before = block.Version;
        var act = () => block.MoveUnder(path, FractionalIndex.Create("a1"), _actorId, _now);
        act.Should().Throw<BusinessRuleException>();
        block.Version.Should().Be(before);
    }

    [CoversMutation(typeof(Block), nameof(Block.MoveToRoot), MutationScenario.NoOp, typeof(FractionalIndex), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void MoveUnder_NoOp_ShouldNotIncrementVersion()
    {
        var block = CreateRootBlock();
        var before = block.Version;
        block.MoveToRoot(FractionalIndex.Create("a0"), _actorId, _now);
        block.Version.Should().Be(before);
    }

    [CoversMutation(typeof(Block), nameof(Block.MoveUnder), MutationScenario.Valid, typeof(BlockAncestorPath), typeof(FractionalIndex), typeof(Guid), typeof(DateTimeOffset))]
    [CoversMutation(typeof(Block), nameof(Block.MoveUnder), MutationScenario.Version, typeof(BlockAncestorPath), typeof(FractionalIndex), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void MoveUnder_ShouldIncrementVersion()
    {
        var block = CreateRootBlock();
        var parentId = Guid.NewGuid();
        var path = BlockAncestorPath.Create(_accountId, _workspaceId, _pageId, parentId, [Guid.NewGuid()]);
        var before = block.Version;
        block.MoveUnder(path, FractionalIndex.Create("a1"), _actorId, _now);
        block.Version.Should().Be(before + 1);
    }

    [CoversMutation(typeof(Block), nameof(Block.MoveToRoot), MutationScenario.Valid, typeof(FractionalIndex), typeof(Guid), typeof(DateTimeOffset))]
    [CoversMutation(typeof(Block), nameof(Block.MoveToRoot), MutationScenario.Version, typeof(FractionalIndex), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void MoveToRoot_ShouldIncrementVersion()
    {
        var block = CreateRootBlock();
        var parentId = Guid.NewGuid();
        var path = BlockAncestorPath.Create(_accountId, _workspaceId, _pageId, parentId, [Guid.NewGuid()]);
        block.MoveUnder(path, FractionalIndex.Create("a1"), _actorId, _now);
        var before = block.Version;
        block.MoveToRoot(FractionalIndex.Create("a2"), _actorId, _now);
        block.Version.Should().Be(before + 1);
    }

    [CoversMutation(typeof(Block), nameof(Block.MoveToRoot), MutationScenario.NoOp, typeof(FractionalIndex), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void MoveToRoot_NoOp_ShouldNotIncrementVersion()
    {
        var block = CreateRootBlock();
        var before = block.Version;
        block.MoveToRoot(FractionalIndex.Create("a0"), _actorId, _now);
        block.Version.Should().Be(before);
    }
}
