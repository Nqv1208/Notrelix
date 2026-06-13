using FluentAssertions;
using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.WorkManagement.BoardGroups;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Domain.WorkManagement.Fields;
using Notrelix.Domain.Billing.Subscriptions;
using Xunit;

namespace Notrelix.Domain.Tests;

public class Phase1AuditTests
{
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _boardId = Guid.NewGuid();
    private readonly Guid _actorId = Guid.NewGuid();
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    #region No-Op Version Guard

    [Fact]
    public void Rename_ShouldNotIncrementVersion_WhenTitleIsSame()
    {
        var board = Board.Create(_workspaceId, _actorId, "Same Title", null, _now);
        var version = board.Version;

        board.Rename("Same Title", _actorId, _now);

        board.Version.Should().Be(version);
        board.DomainEvents.Should().NotContain(e => e is BoardRenamedEvent);
    }

    [Fact]
    public void UpdateDescription_ShouldNotIncrementVersion_WhenDescriptionIsSame()
    {
        var board = Board.Create(_workspaceId, _actorId, "Board", "Same Desc", _now);
        board.ClearDomainEvents();
        var version = board.Version;

        board.UpdateDescription("Same Desc", _actorId, _now);

        board.Version.Should().Be(version);
    }

    [Fact]
    public void ChangeVisibility_ShouldNotIncrementVersion_WhenVisibilityIsSame()
    {
        var board = Board.Create(_workspaceId, _actorId, "Board", null, _now);
        board.ClearDomainEvents();
        var version = board.Version;

        board.ChangeVisibility(BoardVisibility.Workspace, _actorId, _now);

        board.Version.Should().Be(version);
    }

    [Fact]
    public void Archive_ShouldNotIncrementVersion_WhenAlreadyArchived()
    {
        var board = Board.Create(_workspaceId, _actorId, "Board", null, _now);
        board.Archive(_actorId, _now);
        var version = board.Version;

        board.Archive(_actorId, _now);

        board.Version.Should().Be(version);
    }

    [Fact]
    public void Unarchive_ShouldNotIncrementVersion_WhenNotArchived()
    {
        var board = Board.Create(_workspaceId, _actorId, "Board", null, _now);
        board.ClearDomainEvents();
        var version = board.Version;

        board.Unarchive(_actorId, _now);

        board.Version.Should().Be(version);
    }

    [Fact]
    public void BoardItemRename_ShouldNotIncrementVersion_WhenNameIsSame()
    {
        var position = FractionalIndex.Create("a0");
        var item = BoardItem.Create(_workspaceId, _boardId, Guid.NewGuid(), "Item", position, _actorId, _now);
        var version = item.Version;

        item.Rename("Item", _actorId, _now);

        item.Version.Should().Be(version);
        item.DomainEvents.Should().NotContain(e => e is BoardItemRenamedEvent);
    }

    [Fact]
    public void BoardItemMoveToGroup_ShouldNotIncrementVersion_WhenGroupAndPositionAreSame()
    {
        var groupId = Guid.NewGuid();
        var position = FractionalIndex.Create("a0");
        var item = BoardItem.Create(_workspaceId, _boardId, groupId, "Item", position, _actorId, _now);
        var version = item.Version;

        var boardGroupRef = new BoardGroupRef(_workspaceId, _boardId, groupId);
        item.MoveToGroup(boardGroupRef, position, _actorId, _now);

        item.Version.Should().Be(version);
    }

    [Fact]
    public void CancelImmediately_ShouldNotIncrementVersion_WhenAlreadyCanceled()
    {
        var sub = Subscription.Create(_workspaceId, Guid.NewGuid(), SubscriptionTier.Pro, _now, _now.AddDays(30), _actorId, _now);
        sub.CancelImmediately(_actorId, _now);
        var version = sub.Version;

        sub.CancelImmediately(_actorId, _now);

        sub.Version.Should().Be(version);
    }

    [Fact]
    public void ScheduleCancellation_ShouldNotIncrementVersion_WhenAlreadyScheduled()
    {
        var sub = Subscription.Create(_workspaceId, Guid.NewGuid(), SubscriptionTier.Pro, _now, _now.AddDays(30), _actorId, _now);
        sub.ScheduleCancellation(_actorId, _now);
        var version = sub.Version;

        sub.ScheduleCancellation(_actorId, _now);

        sub.Version.Should().Be(version);
    }

    [Fact]
    public void Expire_ShouldNotIncrementVersion_WhenAlreadyExpired()
    {
        var sub = Subscription.Create(_workspaceId, Guid.NewGuid(), SubscriptionTier.Pro, _now, _now.AddDays(30), _actorId, _now);
        sub.Expire(_actorId, _now);
        var version = sub.Version;

        sub.Expire(_actorId, _now);

        sub.Version.Should().Be(version);
    }

    [Fact]
    public void MarkPastDue_ShouldNotIncrementVersion_WhenAlreadyPastDue()
    {
        var sub = Subscription.Create(_workspaceId, Guid.NewGuid(), SubscriptionTier.Pro, _now, _now.AddDays(30), _actorId, _now);
        sub.MarkPastDue(_actorId, _now);
        var version = sub.Version;

        sub.MarkPastDue(_actorId, _now);

        sub.Version.Should().Be(version);
    }

    #endregion

    #region SoftDelete / Restore Versioning

    [Fact]
    public void BoardSoftDelete_ShouldIncrementVersion_AndRaiseEvent()
    {
        var board = Board.Create(_workspaceId, _actorId, "Board", null, _now);
        var version = board.Version;

        board.SoftDelete(_actorId, _now);

        board.IsDeleted.Should().BeTrue();
        board.Version.Should().Be(version + 1);
        board.DomainEvents.Should().ContainSingle(e => e is BoardSoftDeletedEvent);
    }

    [Fact]
    public void BoardRestore_ShouldIncrementVersion_AndRaiseEvent()
    {
        var board = Board.Create(_workspaceId, _actorId, "Board", null, _now);
        board.SoftDelete(_actorId, _now);
        board.ClearDomainEvents();
        var version = board.Version;

        board.Restore(_actorId, _now);

        board.IsDeleted.Should().BeFalse();
        board.Version.Should().Be(version + 1);
        board.DomainEvents.Should().ContainSingle(e => e is BoardRestoredEvent);
    }

    [Fact]
    public void BoardSoftDelete_ShouldNotIncrementOrRaiseEvent_WhenAlreadyDeleted()
    {
        var board = Board.Create(_workspaceId, _actorId, "Board", null, _now);
        board.SoftDelete(_actorId, _now);
        board.ClearDomainEvents();
        var version = board.Version;

        board.SoftDelete(_actorId, _now);

        board.Version.Should().Be(version);
        board.DomainEvents.Should().NotContain(e => e is BoardSoftDeletedEvent);
    }

    [Fact]
    public void BoardRestore_ShouldNotIncrementOrRaiseEvent_WhenNotDeleted()
    {
        var board = Board.Create(_workspaceId, _actorId, "Board", null, _now);
        board.ClearDomainEvents();
        var version = board.Version;

        board.Restore(_actorId, _now);

        board.Version.Should().Be(version);
        board.DomainEvents.Should().NotContain(e => e is BoardRestoredEvent);
    }

    [Fact]
    public void BoardItemSoftDelete_ShouldIncrementVersion_AndRaiseEvent()
    {
        var item = BoardItem.Create(_workspaceId, _boardId, Guid.NewGuid(), "Item", FractionalIndex.Create("a0"), _actorId, _now);
        var version = item.Version;

        item.SoftDelete(_actorId, _now);

        item.IsDeleted.Should().BeTrue();
        item.Version.Should().Be(version + 1);
        item.DomainEvents.Should().ContainSingle(e => e is BoardItemSoftDeletedEvent);
    }

    #endregion

    #region Actor Metadata Propagation

    [Fact]
    public void Event_ShouldCarryActorUserId_WhenAggregateMethodProvidesActor()
    {
        var board = Board.Create(_workspaceId, _actorId, "Board", null, _now);
        board.ClearDomainEvents();

        board.Rename("Renamed", _actorId, _now);

        var evt = (IDomainEvent)board.DomainEvents.Single(e => e is BoardRenamedEvent);
        evt.ActorUserId.Should().Be(_actorId);
    }

    [Fact]
    public void Event_ShouldCarryActorUserId_ForSoftDelete()
    {
        var board = Board.Create(_workspaceId, _actorId, "Board", null, _now);
        board.ClearDomainEvents();

        board.SoftDelete(_actorId, _now);

        var evt = (IDomainEvent)board.DomainEvents.Single(e => e is BoardSoftDeletedEvent);
        evt.ActorUserId.Should().Be(_actorId);
    }

    [Fact]
    public void Event_ShouldCarryActorUserId_ForRestore()
    {
        var board = Board.Create(_workspaceId, _actorId, "Board", null, _now);
        board.SoftDelete(_actorId, _now);
        board.ClearDomainEvents();

        board.Restore(_actorId, _now);

        var evt = (IDomainEvent)board.DomainEvents.Single(e => e is BoardRestoredEvent);
        evt.ActorUserId.Should().Be(_actorId);
    }

    [Fact]
    public void Event_ShouldCarryNullActor_WhenCreatedBySystem()
    {
        var subscription = Subscription.Create(
            _workspaceId,
            Guid.NewGuid(),
            SubscriptionTier.Pro,
            _now,
            _now.AddDays(30),
            Guid.Empty,  // system-triggered (e.g., plan migration)
            _now);

        var evt = (IDomainEvent)subscription.DomainEvents.Single(e => e is SubscriptionStartedEvent);
        evt.ActorUserId.Should().BeNull();
    }

    #endregion

    #region WorkspaceId Propagation

    [Fact]
    public void Event_ShouldCarryCorrectWorkspaceId()
    {
        var board = Board.Create(_workspaceId, _actorId, "Board", null, _now);

        var evt = (IDomainEvent)board.DomainEvents.Single(e => e is BoardCreatedEvent);
        evt.WorkspaceId.Should().Be(_workspaceId);
    }

    [Fact]
    public void Event_ShouldCarryCorrectWorkspaceId_AfterMutation()
    {
        var board = Board.Create(_workspaceId, _actorId, "Board", null, _now);
        board.ClearDomainEvents();

        board.Rename("Renamed", _actorId, _now);

        var evt = (IDomainEvent)board.DomainEvents.Single(e => e is BoardRenamedEvent);
        evt.WorkspaceId.Should().Be(_workspaceId);
    }

    #endregion

    #region Explicit Create Timestamp

    [Fact]
    public void BoardCreate_ShouldUseExplicitCreatedAt()
    {
        var explicitTime = new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);

        var board = Board.Create(_workspaceId, _actorId, "Board", null, explicitTime);

        board.CreatedAt.Should().Be(explicitTime);
    }

    #endregion
}
