using FluentAssertions;
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Domain.Tests.Freeze;

namespace Notrelix.Domain.Tests.WorkManagement.Items;

public class BoardItemHierarchyTests
{
    private static readonly Guid AccountId = Guid.NewGuid();
    private static readonly Guid WsA = Guid.NewGuid();
    private static readonly Guid BoardA = Guid.NewGuid();
    private static readonly Guid GroupA = Guid.NewGuid();
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public void CreateRoot_ShouldSetNullParent_LevelZero_AndRaiseCreationEvent()
    {
        var item = BoardItem.CreateRoot(AccountId, WsA, BoardA, GroupA, "Root", FractionalIndex.Create("a0"), Actor, Now);

        item.ParentItemId.Should().BeNull();
        item.ItemLevel.Should().Be(0);
        var evt = item.DomainEvents.OfType<BoardItemCreatedDomainEvent>().Single();
        evt.ParentItemId.Should().BeNull();
        evt.ItemLevel.Should().Be(0);
        evt.GroupId.Should().Be(GroupA);
    }

    [Fact]
    public void CreateChild_ShouldDeriveParentAndLevelFromPath()
    {
        var parentId = Guid.NewGuid();
        var parentPath = ItemParentPath.Create(AccountId, WsA, BoardA, parentId, 2, new[] { Guid.NewGuid(), Guid.NewGuid() });

        var child = BoardItem.CreateChild(parentPath, GroupA, "Child", FractionalIndex.Create("a0"), Actor, Now);

        child.AccountId.Should().Be(AccountId);
        child.WorkspaceId.Should().Be(WsA);
        child.BoardId.Should().Be(BoardA);
        child.ParentItemId.Should().Be(parentId);
        child.ItemLevel.Should().Be(3);
        var evt = child.DomainEvents.OfType<BoardItemCreatedDomainEvent>().Single();
        evt.ParentItemId.Should().Be(parentId);
        evt.ItemLevel.Should().Be(3);
        evt.GroupId.Should().Be(GroupA);
    }

    [Fact]
    public void CreateChild_WithDirectParent_ShouldUseLevelOne()
    {
        var parentId = Guid.NewGuid();
        var parentPath = ItemParentPath.Create(AccountId, WsA, BoardA, parentId, 0, Array.Empty<Guid>());

        var child = BoardItem.CreateChild(parentPath, GroupA, "Child", FractionalIndex.Create("a0"), Actor, Now);

        child.ItemLevel.Should().Be(1);
    }

    [Fact]
    public void CreateChild_WithDifferentGroupThanParent_ShouldBeAllowed()
    {
        var parentId = Guid.NewGuid();
        var parentGroup = Guid.NewGuid();
        var childGroup = Guid.NewGuid();
        var parent = BoardItem.CreateRoot(AccountId, WsA, BoardA, parentGroup, "Parent", FractionalIndex.Create("a0"), Actor, Now);
        var parentPath = ItemParentPath.Create(AccountId, WsA, BoardA, parent.Id, 0, Array.Empty<Guid>());

        var child = BoardItem.CreateChild(parentPath, childGroup, "Child", FractionalIndex.Create("a0"), Actor, Now);

        child.ParentItemId.Should().Be(parent.Id);
        child.GroupId.Should().Be(childGroup);
        child.GroupId.Should().NotBe(parent.GroupId);
    }

    [Fact]
    public void CreateChild_WithNullPath_ShouldThrow()
    {
        Action act = () => BoardItem.CreateChild(null!, GroupA, "Child", FractionalIndex.Create("a0"), Actor, Now);
        act.Should().Throw<BusinessRuleException>();
    }

    [CoversMutation(typeof(BoardItem), "MoveUnder(Notrelix.Domain.WorkManagement.Items.ItemParentPath,System.Guid,System.DateTimeOffset)", MutationScenario.FailureAtomicity)]
    [Fact]
    public void MoveUnder_ShouldNotChangeState_WhenCycleDetected()
    {
        var item = BoardItem.CreateRoot(AccountId, WsA, BoardA, GroupA, "Item", FractionalIndex.Create("a0"), Actor, Now);
        item.MoveUnder(ItemParentPath.Create(AccountId, WsA, BoardA, Guid.NewGuid(), 0, Array.Empty<Guid>()), Actor, Now);
        ((IHasDomainEvents)item).ClearDomainEvents();
        var version = item.Version;
        var previousParent = item.ParentItemId;
        var previousLevel = item.ItemLevel;

        var parentPath = ItemParentPath.Create(AccountId, WsA, BoardA, Guid.NewGuid(), 1, new[] { item.Id });
        var act = () => item.MoveUnder(parentPath, Actor, Now);

        act.Should().Throw<BusinessRuleException>();
        item.ParentItemId.Should().Be(previousParent);
        item.ItemLevel.Should().Be(previousLevel);
        item.Version.Should().Be(version);
        item.DomainEvents.Should().NotContain(e => e is BoardItemParentChangedDomainEvent);
    }

    [CoversMutation(typeof(BoardItem), "MoveUnder(Notrelix.Domain.WorkManagement.Items.ItemParentPath,System.Guid,System.DateTimeOffset)", MutationScenario.FailureAtomicity)]
    [Fact]
    public void MoveUnder_ShouldNotChangeState_WhenScopeMismatch()
    {
        var item = BoardItem.CreateRoot(AccountId, WsA, BoardA, GroupA, "Item", FractionalIndex.Create("a0"), Actor, Now);
        var version = item.Version;

        var parentPath = ItemParentPath.Create(AccountId, WsA, Guid.NewGuid(), Guid.NewGuid(), 0, Array.Empty<Guid>());
        var act = () => item.MoveUnder(parentPath, Actor, Now);

        act.Should().Throw<BusinessRuleException>();
        item.ParentItemId.Should().BeNull();
        item.ItemLevel.Should().Be(0);
        item.Version.Should().Be(version);
    }
}
