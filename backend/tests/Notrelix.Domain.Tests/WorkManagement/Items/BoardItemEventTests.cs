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

    private static BoardItem CreateRootItem()
    {
        return BoardItem.CreateRoot(AccountId, WsA, BoardA, GroupA, "Item", FractionalIndex.Create("a0"), Actor, Now);
    }

    private static ItemParentPath Path(Guid parentId, int level = 0, params Guid[] ancestors)
    {
        return ItemParentPath.Create(AccountId, WsA, BoardA, parentId, level, ancestors);
    }

    [Fact]
    public void BoardItem_Complete_ShouldRaiseEvent()
    {
        var item = CreateRootItem();
        ((IHasDomainEvents)item).ClearDomainEvents();
        var version = item.Version;

        item.Complete(Now, Actor, Now);

        item.Version.Should().Be(version + 1);
        item.CompletedAt.Should().Be(Now);
        item.DomainEvents.Should().ContainSingle(e => e is BoardItemCompletedDomainEvent);
        var evt = (BoardItemCompletedDomainEvent)item.DomainEvents.Single(e => e is BoardItemCompletedDomainEvent);
        evt.CompletedAt.Should().Be(Now);
        evt.CompletedBy.Should().Be(Actor);
    }

    [Fact]
    public void BoardItem_Complete_WhenAlreadyCompleted_ShouldNotRaiseEventOrChangeTimestamp()
    {
        var item = CreateRootItem();
        item.Complete(Now, Actor, Now);
        ((IHasDomainEvents)item).ClearDomainEvents();
        var version = item.Version;

        item.Complete(Now.AddHours(1), Actor, Now.AddHours(1));

        item.Version.Should().Be(version);
        item.CompletedAt.Should().Be(Now);
        item.DomainEvents.Should().NotContain(e => e is BoardItemCompletedDomainEvent);
    }

    [Fact]
    public void BoardItem_Complete_WithDefaultTimestamp_ShouldThrow()
    {
        var item = CreateRootItem();
        ((IHasDomainEvents)item).ClearDomainEvents();

        Action act = () => item.Complete(default, Actor, Now);

        act.Should().Throw<BusinessRuleException>();
        item.DomainEvents.Should().NotContain(e => e is BoardItemCompletedDomainEvent);
    }

    [Fact]
    public void BoardItem_Reopen_ShouldClearCompletedAtAndRaiseEvent()
    {
        var item = CreateRootItem();
        item.Complete(Now, Actor, Now);
        ((IHasDomainEvents)item).ClearDomainEvents();
        var version = item.Version;

        item.Reopen(Actor, Now.AddMinutes(5));

        item.Version.Should().Be(version + 1);
        item.CompletedAt.Should().BeNull();
        item.DomainEvents.Should().ContainSingle(e => e is BoardItemReopenedDomainEvent);
        var evt = (BoardItemReopenedDomainEvent)item.DomainEvents.Single(e => e is BoardItemReopenedDomainEvent);
        evt.ReopenedBy.Should().Be(Actor);
    }

    [Fact]
    public void BoardItem_Reopen_WhenNotCompleted_ShouldNotRaiseEvent()
    {
        var item = CreateRootItem();
        ((IHasDomainEvents)item).ClearDomainEvents();
        var version = item.Version;

        item.Reopen(Actor, Now);

        item.Version.Should().Be(version);
        item.DomainEvents.Should().NotContain(e => e is BoardItemReopenedDomainEvent);
    }

    [Fact]
    public void BoardItem_SetTimeline_ShouldRaiseEvent()
    {
        var item = CreateRootItem();
        ((IHasDomainEvents)item).ClearDomainEvents();
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
        var item = BoardItem.CreateRoot(AccountId, WsA, BoardA, GroupA, "Item", FractionalIndex.Create("a0"), Actor, Now, startedAt: Now, dueAt: Now.AddDays(7));
        ((IHasDomainEvents)item).ClearDomainEvents();
        var version = item.Version;

        item.SetTimeline(Now, Now.AddDays(7), Actor, Now);

        item.Version.Should().Be(version);
        item.DomainEvents.Should().NotContain(e => e is BoardItemTimelineSetDomainEvent);
    }

    [Fact]
    public void BoardItem_MoveUnder_ShouldRaiseEvent()
    {
        var item = CreateRootItem();
        ((IHasDomainEvents)item).ClearDomainEvents();
        var version = item.Version;
        var parentId = Guid.NewGuid();

        item.MoveUnder(Path(parentId), Actor, Now);

        item.Version.Should().Be(version + 1);
        item.ParentItemId.Should().Be(parentId);
        item.ItemLevel.Should().Be(1);
        item.DomainEvents.Should().ContainSingle(e => e is BoardItemParentChangedDomainEvent);
        var evt = (BoardItemParentChangedDomainEvent)item.DomainEvents.Single(e => e is BoardItemParentChangedDomainEvent);
        evt.NewParentItemId.Should().Be(parentId);
        evt.NewLevel.Should().Be(1);
        evt.PreviousParentItemId.Should().BeNull();
        evt.PreviousLevel.Should().Be(0);
    }

    [Fact]
    public void BoardItem_MoveUnder_ShouldDeriveLevelFromParentPath()
    {
        var item = CreateRootItem();
        ((IHasDomainEvents)item).ClearDomainEvents();
        var parentId = Guid.NewGuid();
        var grandparentId = Guid.NewGuid();

        item.MoveUnder(Path(parentId, level: 2, grandparentId, Guid.NewGuid()), Actor, Now);

        item.ParentItemId.Should().Be(parentId);
        item.ItemLevel.Should().Be(3);
    }

    [Fact]
    public void BoardItem_MoveUnder_WithOwnId_ShouldThrow()
    {
        var item = CreateRootItem();

        var act = () => item.MoveUnder(Path(item.Id), Actor, Now);

        act.Should().Throw<BusinessRuleException>().WithMessage("*own parent*");
    }

    [Fact]
    public void BoardItem_MoveUnder_WithCycle_ShouldThrow()
    {
        var item = CreateRootItem();
        var parentId = Guid.NewGuid();

        var act = () => item.MoveUnder(Path(parentId, level: 1, item.Id), Actor, Now);

        act.Should().Throw<BusinessRuleException>().WithMessage("*cycle*");
        item.ParentItemId.Should().BeNull();
        item.ItemLevel.Should().Be(0);
    }

    [Fact]
    public void BoardItem_MoveUnder_WithDifferentAccount_ShouldThrow()
    {
        var item = CreateRootItem();
        var parentPath = ItemParentPath.Create(Guid.NewGuid(), WsA, BoardA, Guid.NewGuid(), 0, Array.Empty<Guid>());

        var act = () => item.MoveUnder(parentPath, Actor, Now);

        act.Should().Throw<BusinessRuleException>().WithMessage("*account*");
        item.ParentItemId.Should().BeNull();
    }

    [Fact]
    public void BoardItem_MoveUnder_WithDifferentWorkspace_ShouldThrow()
    {
        var item = CreateRootItem();
        var parentPath = ItemParentPath.Create(AccountId, Guid.NewGuid(), BoardA, Guid.NewGuid(), 0, Array.Empty<Guid>());

        var act = () => item.MoveUnder(parentPath, Actor, Now);

        act.Should().Throw<BusinessRuleException>().WithMessage("*workspace*");
        item.ParentItemId.Should().BeNull();
    }

    [Fact]
    public void BoardItem_MoveUnder_WithDifferentBoard_ShouldThrow()
    {
        var item = CreateRootItem();
        var parentPath = ItemParentPath.Create(AccountId, WsA, Guid.NewGuid(), Guid.NewGuid(), 0, Array.Empty<Guid>());

        var act = () => item.MoveUnder(parentPath, Actor, Now);

        act.Should().Throw<BusinessRuleException>().WithMessage("*board*");
        item.ParentItemId.Should().BeNull();
    }

    [Fact]
    public void BoardItem_MoveUnder_WhenSameParentAndLevel_ShouldNotRaiseEvent()
    {
        var item = CreateRootItem();
        var parentId = Guid.NewGuid();
        item.MoveUnder(Path(parentId), Actor, Now);
        ((IHasDomainEvents)item).ClearDomainEvents();
        var version = item.Version;

        item.MoveUnder(Path(parentId), Actor, Now);

        item.Version.Should().Be(version);
        item.DomainEvents.Should().NotContain(e => e is BoardItemParentChangedDomainEvent);
    }

    [Fact]
    public void BoardItem_MoveToRoot_ShouldClearParentAndRaiseEvent()
    {
        var item = CreateRootItem();
        var parentId = Guid.NewGuid();
        item.MoveUnder(Path(parentId), Actor, Now);
        ((IHasDomainEvents)item).ClearDomainEvents();
        var version = item.Version;

        item.MoveToRoot(Actor, Now);

        item.ParentItemId.Should().BeNull();
        item.ItemLevel.Should().Be(0);
        item.Version.Should().Be(version + 1);
        item.DomainEvents.Should().ContainSingle(e => e is BoardItemParentChangedDomainEvent);
        var evt = (BoardItemParentChangedDomainEvent)item.DomainEvents.Single(e => e is BoardItemParentChangedDomainEvent);
        evt.PreviousParentItemId.Should().Be(parentId);
        evt.PreviousLevel.Should().Be(1);
        evt.NewParentItemId.Should().BeNull();
        evt.NewLevel.Should().Be(0);
    }

    [Fact]
    public void BoardItem_MoveToRoot_WhenAlreadyRoot_ShouldNotRaiseEvent()
    {
        var item = CreateRootItem();
        ((IHasDomainEvents)item).ClearDomainEvents();
        var version = item.Version;

        item.MoveToRoot(Actor, Now);

        item.Version.Should().Be(version);
        item.DomainEvents.Should().NotContain(e => e is BoardItemParentChangedDomainEvent);
    }
}
