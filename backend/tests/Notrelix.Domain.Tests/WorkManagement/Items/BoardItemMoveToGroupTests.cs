using FluentAssertions;
using Notrelix.Domain.WorkManagement.BoardGroups;
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Domain.Tests.Freeze;

namespace Notrelix.Domain.Tests.WorkManagement.Items;

public class BoardItemMoveToGroupTests
{
    private static readonly Guid WsA = Guid.NewGuid();
    private static readonly Guid WsB = Guid.NewGuid();
    private static readonly Guid BoardA = Guid.NewGuid();
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [CoversMutation(typeof(BoardItem), "MoveToGroup(Notrelix.Domain.WorkManagement.BoardGroups.BoardGroupRef,Notrelix.Domain.SharedKernel.Ordering.FractionalIndex,System.Guid,System.DateTimeOffset)", MutationScenario.Valid)]
    [Fact]
    public void MoveToGroup_WithMatchingWorkspaceAndBoard_ShouldSucceed()
    {
        var groupId = Guid.NewGuid();
        var item = BoardItem.CreateRoot(Guid.NewGuid(), WsA, BoardA, groupId, "Item", FractionalIndex.Create("a0"), Actor, Now);
        ((IHasDomainEvents)item).ClearDomainEvents();

        var newGroup = Guid.NewGuid();
        var newPosition = FractionalIndex.Create("a1");
        var groupRef = new BoardGroupRef(Guid.NewGuid(), WsA, BoardA, newGroup);

        item.MoveToGroup(groupRef, newPosition, Actor, Now);

        item.GroupId.Should().Be(newGroup);
        item.Position.Should().Be(newPosition);
        item.DomainEvents.Should().ContainSingle(e => e is BoardItemMovedDomainEvent);
        item.Version.Should().Be(2);
    }

    [CoversMutation(typeof(BoardItem), "MoveToGroup(Notrelix.Domain.WorkManagement.BoardGroups.BoardGroupRef,Notrelix.Domain.SharedKernel.Ordering.FractionalIndex,System.Guid,System.DateTimeOffset)", MutationScenario.Invalid)]
    [Fact]
    public void MoveToGroup_WithMismatchedWorkspace_ShouldThrow()
    {
        var item = BoardItem.CreateRoot(Guid.NewGuid(), WsA, BoardA, Guid.NewGuid(), "Item", FractionalIndex.Create("a0"), Actor, Now);
        var groupRef = new BoardGroupRef(Guid.NewGuid(), WsB, BoardA, Guid.NewGuid());

        var act = () => item.MoveToGroup(groupRef, FractionalIndex.Create("a1"), Actor, Now);
        act.Should().Throw<BusinessRuleException>();
    }

    [CoversMutation(typeof(BoardItem), "MoveToGroup(Notrelix.Domain.WorkManagement.BoardGroups.BoardGroupRef,Notrelix.Domain.SharedKernel.Ordering.FractionalIndex,System.Guid,System.DateTimeOffset)", MutationScenario.Invalid)]
    [Fact]
    public void MoveToGroup_WithMismatchedBoard_ShouldThrow()
    {
        var item = BoardItem.CreateRoot(Guid.NewGuid(), WsA, BoardA, Guid.NewGuid(), "Item", FractionalIndex.Create("a0"), Actor, Now);
        var groupRef = new BoardGroupRef(Guid.NewGuid(), WsA, Guid.NewGuid(), Guid.NewGuid());

        var act = () => item.MoveToGroup(groupRef, FractionalIndex.Create("a1"), Actor, Now);
        act.Should().Throw<BusinessRuleException>();
    }

    [CoversMutation(typeof(BoardItem), "MoveToGroup(Notrelix.Domain.WorkManagement.BoardGroups.BoardGroupRef,Notrelix.Domain.SharedKernel.Ordering.FractionalIndex,System.Guid,System.DateTimeOffset)", MutationScenario.Version)]
    [Fact]
    public void MoveToGroup_WithSameGroupAndPosition_ShouldNotIncrementVersion()
    {
        var groupId = Guid.NewGuid();
        var position = FractionalIndex.Create("a0");
        var item = BoardItem.CreateRoot(Guid.NewGuid(), WsA, BoardA, groupId, "Item", position, Actor, Now);
        var version = item.Version;

        var groupRef = new BoardGroupRef(Guid.NewGuid(), WsA, BoardA, groupId);
        item.MoveToGroup(groupRef, position, Actor, Now);

        item.Version.Should().Be(version);
    }
}
