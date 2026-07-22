using FluentAssertions;
using Notrelix.Domain.WorkManagement.Boards;

namespace Notrelix.Domain.Tests.WorkManagement;

public class BoardIdempotencyTests
{
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _actorId = Guid.NewGuid();
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    [Fact]
    public void Rename_ShouldNotIncrementVersion_WhenTitleIsSame()
    {
        var board = Board.Create(Guid.NewGuid(), _workspaceId, _actorId, "Same Title", null, _now);
        var version = board.Version;

        board.Rename("Same Title", _actorId, _now);

        board.Version.Should().Be(version);
        board.DomainEvents.Should().NotContain(e => e is BoardRenamedDomainEvent);
    }

    [Fact]
    public void UpdateDescription_ShouldNotIncrementVersion_WhenDescriptionIsSame()
    {
        var board = Board.Create(Guid.NewGuid(), _workspaceId, _actorId, "Board", "Same Desc", _now);
        board.ClearDomainEvents();
        var version = board.Version;

        board.UpdateDescription("Same Desc", _actorId, _now);

        board.Version.Should().Be(version);
    }

    [Fact]
    public void ChangeVisibility_ShouldNotIncrementVersion_WhenVisibilityIsSame()
    {
        var board = Board.Create(Guid.NewGuid(), _workspaceId, _actorId, "Board", null, _now);
        board.ClearDomainEvents();
        var version = board.Version;

        board.ChangeVisibility(BoardVisibility.Workspace, _actorId, _now);

        board.Version.Should().Be(version);
    }

    [Fact]
    public void Archive_ShouldNotIncrementVersion_WhenAlreadyArchived()
    {
        var board = Board.Create(Guid.NewGuid(), _workspaceId, _actorId, "Board", null, _now);
        board.Archive(_actorId, _now);
        var version = board.Version;

        board.Archive(_actorId, _now);

        board.Version.Should().Be(version);
    }

    [Fact]
    public void Unarchive_ShouldNotIncrementVersion_WhenNotArchived()
    {
        var board = Board.Create(Guid.NewGuid(), _workspaceId, _actorId, "Board", null, _now);
        board.ClearDomainEvents();
        var version = board.Version;

        board.Unarchive(_actorId, _now);

        board.Version.Should().Be(version);
    }

    [Fact]
    public void SoftDelete_ShouldIncrementVersion_AndRaiseEvent()
    {
        var board = Board.Create(Guid.NewGuid(), _workspaceId, _actorId, "Board", null, _now);
        var version = board.Version;

        board.SoftDelete(_actorId, _now);

        board.IsDeleted.Should().BeTrue();
        board.Version.Should().Be(version + 1);
        board.DomainEvents.Should().ContainSingle(e => e is BoardSoftDeletedDomainEvent);
    }

    [Fact]
    public void Restore_ShouldIncrementVersion_AndRaiseEvent()
    {
        var board = Board.Create(Guid.NewGuid(), _workspaceId, _actorId, "Board", null, _now);
        board.SoftDelete(_actorId, _now);
        board.ClearDomainEvents();
        var version = board.Version;

        board.Restore(_actorId, _now);

        board.IsDeleted.Should().BeFalse();
        board.Version.Should().Be(version + 1);
        board.DomainEvents.Should().ContainSingle(e => e is BoardRestoredDomainEvent);
    }

    [Fact]
    public void SoftDelete_ShouldNotIncrementOrRaiseEvent_WhenAlreadyDeleted()
    {
        var board = Board.Create(Guid.NewGuid(), _workspaceId, _actorId, "Board", null, _now);
        board.SoftDelete(_actorId, _now);
        board.ClearDomainEvents();
        var version = board.Version;

        board.SoftDelete(_actorId, _now);

        board.Version.Should().Be(version);
        board.DomainEvents.Should().NotContain(e => e is BoardSoftDeletedDomainEvent);
    }

    [Fact]
    public void Restore_ShouldNotIncrementOrRaiseEvent_WhenNotDeleted()
    {
        var board = Board.Create(Guid.NewGuid(), _workspaceId, _actorId, "Board", null, _now);
        board.ClearDomainEvents();
        var version = board.Version;

        board.Restore(_actorId, _now);

        board.Version.Should().Be(version);
        board.DomainEvents.Should().NotContain(e => e is BoardRestoredDomainEvent);
    }

    [Fact]
    public void DomainEvent_ShouldCarryActorUserId_WhenAggregateMethodProvidesActor()
    {
        var board = Board.Create(Guid.NewGuid(), _workspaceId, _actorId, "Board", null, _now);
        board.ClearDomainEvents();

        board.Rename("Renamed", _actorId, _now);

        var evt = (IDomainEvent)board.DomainEvents.Single(e => e is BoardRenamedDomainEvent);
        evt.ActorUserId.Should().Be(_actorId);
    }

    [Fact]
    public void DomainEvent_ShouldCarryActorUserId_ForSoftDelete()
    {
        var board = Board.Create(Guid.NewGuid(), _workspaceId, _actorId, "Board", null, _now);
        board.ClearDomainEvents();

        board.SoftDelete(_actorId, _now);

        var evt = (IDomainEvent)board.DomainEvents.Single(e => e is BoardSoftDeletedDomainEvent);
        evt.ActorUserId.Should().Be(_actorId);
    }

    [Fact]
    public void DomainEvent_ShouldCarryActorUserId_ForRestore()
    {
        var board = Board.Create(Guid.NewGuid(), _workspaceId, _actorId, "Board", null, _now);
        board.SoftDelete(_actorId, _now);
        board.ClearDomainEvents();

        board.Restore(_actorId, _now);

        var evt = (IDomainEvent)board.DomainEvents.Single(e => e is BoardRestoredDomainEvent);
        evt.ActorUserId.Should().Be(_actorId);
    }

    [Fact]
    public void DomainEvent_ShouldCarryCorrectWorkspaceId()
    {
        var board = Board.Create(Guid.NewGuid(), _workspaceId, _actorId, "Board", null, _now);

        var evt = (IWorkspaceScoped)board.DomainEvents.Single(e => e is BoardCreatedDomainEvent);
        evt.WorkspaceId.Should().Be(_workspaceId);
    }

    [Fact]
    public void DomainEvent_ShouldCarryCorrectWorkspaceId_AfterMutation()
    {
        var board = Board.Create(Guid.NewGuid(), _workspaceId, _actorId, "Board", null, _now);
        board.ClearDomainEvents();

        board.Rename("Renamed", _actorId, _now);

        var evt = (IWorkspaceScoped)board.DomainEvents.Single(e => e is BoardRenamedDomainEvent);
        evt.WorkspaceId.Should().Be(_workspaceId);
    }

    [Fact]
    public void BoardCreate_ShouldUseExplicitCreatedAt()
    {
        var explicitTime = new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);

        var board = Board.Create(Guid.NewGuid(), _workspaceId, _actorId, "Board", null, explicitTime);

        board.CreatedAt.Should().Be(explicitTime);
    }
}
