using FluentAssertions;
using Notrelix.Domain.WorkManagement.Items;

namespace Notrelix.Domain.Tests.WorkManagement.Items;

public class BoardItemEventTests
{
    private static readonly Guid AccountId = Guid.NewGuid();
    private static readonly Guid WsA = Guid.NewGuid();
    private static readonly Guid BoardA = Guid.NewGuid();
    private static readonly Guid GroupA = Guid.NewGuid();
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public void BoardItem_Complete_ShouldRaiseEvent()
    {
        var item = BoardItem.Create(AccountId, WsA, BoardA, GroupA, "Item", FractionalIndex.Create("a0"), Actor, Now);
        item.ClearDomainEvents();
        var version = item.Version;

        item.Complete(Now, Actor, Now);

        item.Version.Should().Be(version + 1);
        item.DomainEvents.Should().ContainSingle(e => e is BoardItemCompletedDomainEvent);
        var evt = (BoardItemCompletedDomainEvent)item.DomainEvents.Single(e => e is BoardItemCompletedDomainEvent);
        evt.CompletedAt.Should().Be(Now);
        evt.CompletedBy.Should().Be(Actor);
    }

    [Fact]
    public void BoardItem_Complete_WhenSameValue_ShouldNotRaiseEvent()
    {
        var item = BoardItem.Create(AccountId, WsA, BoardA, GroupA, "Item", FractionalIndex.Create("a0"), Actor, Now);
        item.Complete(Now, Actor, Now);
        item.ClearDomainEvents();
        var version = item.Version;

        item.Complete(Now, Actor, Now);

        item.Version.Should().Be(version);
        item.DomainEvents.Should().NotContain(e => e is BoardItemCompletedDomainEvent);
    }

    [Fact]
    public void BoardItem_SetTimeline_ShouldRaiseEvent()
    {
        var item = BoardItem.Create(AccountId, WsA, BoardA, GroupA, "Item", FractionalIndex.Create("a0"), Actor, Now);
        item.ClearDomainEvents();
        var version = item.Version;

        item.SetTimeline(Now, Now.AddDays(7), Actor, Now);

        item.Version.Should().Be(version + 1);
        item.DomainEvents.Should().ContainSingle(e => e is BoardItemTimelineSetDomainEvent);
        var evt = (BoardItemTimelineSetDomainEvent)item.DomainEvents.Single(e => e is BoardItemTimelineSetDomainEvent);
        evt.StartedAt.Should().Be(Now);
        evt.DueAt.Should().Be(Now.AddDays(7));
    }

    [Fact]
    public void BoardItem_SetTimeline_WhenSameValue_ShouldNotRaiseEvent()
    {
        var item = BoardItem.Create(AccountId, WsA, BoardA, GroupA, "Item", FractionalIndex.Create("a0"), Actor, Now, startedAt: Now, dueAt: Now.AddDays(7));
        item.ClearDomainEvents();
        var version = item.Version;

        item.SetTimeline(Now, Now.AddDays(7), Actor, Now);

        item.Version.Should().Be(version);
        item.DomainEvents.Should().NotContain(e => e is BoardItemTimelineSetDomainEvent);
    }

    [Fact]
    public void BoardItem_AssignParentItem_ShouldRaiseEvent()
    {
        var item = BoardItem.Create(AccountId, WsA, BoardA, GroupA, "Item", FractionalIndex.Create("a0"), Actor, Now);
        item.ClearDomainEvents();
        var version = item.Version;
        var parentId = Guid.NewGuid();

        var chain = new Dictionary<Guid, ItemParentSnapshot>
        {
            [parentId] = new ItemParentSnapshot(parentId, BoardA, null)
        };
        item.AssignParentItem(parentId, 1, chain, Actor, Now);

        item.Version.Should().Be(version + 1);
        item.ParentItemId.Should().Be(parentId);
        item.ItemLevel.Should().Be(1);
        item.DomainEvents.Should().ContainSingle(e => e is BoardItemParentAssignedDomainEvent);
        var evt = (BoardItemParentAssignedDomainEvent)item.DomainEvents.Single(e => e is BoardItemParentAssignedDomainEvent);
        evt.ParentItemId.Should().Be(parentId);
        evt.ItemLevel.Should().Be(1);
    }

    [Fact]
    public void BoardItem_AssignParentItem_WithOwnId_ShouldThrow()
    {
        var item = BoardItem.Create(AccountId, WsA, BoardA, GroupA, "Item", FractionalIndex.Create("a0"), Actor, Now);

        var act = () => item.AssignParentItem(item.Id, 0, new Dictionary<Guid, ItemParentSnapshot>(), Actor, Now);
        act.Should().Throw<BusinessRuleException>().WithMessage("*own parent*");
    }

    [Fact]
    public void BoardItem_AssignParentItem_WithCycle_ShouldThrow()
    {
        var item = BoardItem.Create(AccountId, WsA, BoardA, GroupA, "Item", FractionalIndex.Create("a0"), Actor, Now);

        var grandparent = Guid.NewGuid();
        var parent = Guid.NewGuid();
        var chain = new Dictionary<Guid, ItemParentSnapshot>
        {
            [parent] = new ItemParentSnapshot(parent, BoardA, grandparent),
            [grandparent] = new ItemParentSnapshot(grandparent, BoardA, item.Id),
        };

        var act = () => item.AssignParentItem(parent, 1, chain, Actor, Now);
        act.Should().Throw<BusinessRuleException>().WithMessage("*cycle*");
    }

    [Fact]
    public void BoardItem_AssignParentItem_WithNull_ShouldClearParent()
    {
        var item = BoardItem.Create(AccountId, WsA, BoardA, GroupA, "Item", FractionalIndex.Create("a0"), Actor, Now);
        var parentId = Guid.NewGuid();
        var chain = new Dictionary<Guid, ItemParentSnapshot>
        {
            [parentId] = new ItemParentSnapshot(parentId, BoardA, null)
        };
        item.AssignParentItem(parentId, 1, chain, Actor, Now);
        item.ClearDomainEvents();
        var version = item.Version;

        item.AssignParentItem(null, 0, new Dictionary<Guid, ItemParentSnapshot>(), Actor, Now);

        item.ParentItemId.Should().BeNull();
        item.ItemLevel.Should().Be(0);
        item.Version.Should().Be(version + 1);
        item.DomainEvents.Should().ContainSingle(e => e is BoardItemParentAssignedDomainEvent);
    }
}
