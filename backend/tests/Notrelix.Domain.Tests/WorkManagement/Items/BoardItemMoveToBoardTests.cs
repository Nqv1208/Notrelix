using FluentAssertions;
using Notrelix.Domain.WorkManagement.BoardGroups;
using Notrelix.Domain.WorkManagement.Items;

namespace Notrelix.Domain.Tests.WorkManagement.Items;

public class BoardItemMoveToBoardTests
{
    private static readonly Guid Ws = Guid.NewGuid();
    private static readonly Guid BoardA = Guid.NewGuid();
    private static readonly Guid BoardB = Guid.NewGuid();
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private static BoardItem NewItem() =>
        BoardItem.CreateRoot(Guid.NewGuid(), Ws, BoardA, Guid.NewGuid(), "Item", FractionalIndex.Create("a0"), Actor, Now, "A-1", 1);

    [Fact]
    public void MoveToBoard_ChangesBoardGroupKeyAndRaisesOneFact()
    {
        var item = NewItem();
        ((IHasDomainEvents)item).ClearDomainEvents();
        var group = new BoardGroupRef(Guid.NewGuid(), Ws, BoardB, Guid.NewGuid());

        item.MoveToBoard(group, FractionalIndex.Create("a5"), "B-7", 7, Actor, Now.AddMinutes(1));

        item.BoardId.Should().Be(BoardB);
        item.GroupId.Should().Be(group.GroupId);
        item.ItemKey.Should().Be("B-7");
        item.ItemSequence.Should().Be(7);
        item.Version.Should().Be(2);
        item.DomainEvents.Should().ContainSingle(e => e is BoardItemMovedToBoardDomainEvent);
    }

    [Fact]
    public void MoveToBoard_ToTheSameBoard_Throws()
    {
        var item = NewItem();
        var group = new BoardGroupRef(Guid.NewGuid(), Ws, BoardA, Guid.NewGuid());

        var act = () => item.MoveToBoard(group, FractionalIndex.Create("a5"), "A-2", 2, Actor, Now);

        act.Should().Throw<BusinessRuleException>();
        item.Version.Should().Be(1);
    }

    [Fact]
    public void MoveToBoard_ToAnotherWorkspace_Throws()
    {
        var item = NewItem();
        var group = new BoardGroupRef(Guid.NewGuid(), Guid.NewGuid(), BoardB, Guid.NewGuid());

        var act = () => item.MoveToBoard(group, FractionalIndex.Create("a5"), "B-1", 1, Actor, Now);

        act.Should().Throw<BusinessRuleException>();
        item.BoardId.Should().Be(BoardA);
    }

    [Fact]
    public void MoveToBoard_ADeletedItem_Throws()
    {
        var item = NewItem();
        item.Delete(Actor, Now.AddMinutes(1));
        var group = new BoardGroupRef(Guid.NewGuid(), Ws, BoardB, Guid.NewGuid());

        var act = () => item.MoveToBoard(group, FractionalIndex.Create("a5"), "B-1", 1, Actor, Now.AddMinutes(2));

        act.Should().Throw<BusinessRuleException>();
    }
}
