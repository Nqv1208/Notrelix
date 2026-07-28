using FluentAssertions;
using Notrelix.Domain.Tests.Freeze;
using Notrelix.Domain.WorkManagement.BoardGroups;

namespace Notrelix.Domain.Tests.WorkManagement;

[CoversAggregate(typeof(BoardGroup))]
public class BoardGroupTests
{
    [Fact]
    public void UpdateColor_ShouldRaiseColorChangedEvent()
    {
        var accountId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var boardId = Guid.NewGuid();
        var group = BoardGroup.Create(accountId, workspaceId, boardId, "Group", Color.Create("#000000"), FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)group).ClearDomainEvents();

        group.UpdateColor(Color.Create("#FF0000"), Guid.NewGuid(), DateTimeOffset.UtcNow);

        group.Color.Value.Should().Be("#FF0000");
        group.DomainEvents.Should().ContainSingle(e => e is BoardGroupColorChangedDomainEvent);
    }

    [Fact]
    public void SoftDelete_ShouldRaiseEvent_WithBoardId()
    {
        var accountId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var boardId = Guid.NewGuid();
        var group = BoardGroup.Create(accountId, workspaceId, boardId, "Group", Color.Create("#000000"), FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)group).ClearDomainEvents();

        group.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var deletedEvent = group.DomainEvents.OfType<BoardGroupSoftDeletedDomainEvent>().Single();
        deletedEvent.BoardId.Should().Be(boardId);
        deletedEvent.WorkspaceId.Should().Be(workspaceId);
    }

    [Fact]
    public void Restore_ShouldClearIsDeleted_AndRaiseEvent()
    {
        var group = BoardGroup.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Group", Color.Create("#000000"), FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        group.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);
        group.IsDeleted.Should().BeTrue();
        ((IHasDomainEvents)group).ClearDomainEvents();

        group.Restore(Guid.NewGuid(), DateTimeOffset.UtcNow);

        group.IsDeleted.Should().BeFalse();
        group.DomainEvents.Should().ContainSingle(e => e is BoardGroupRestoredDomainEvent);
    }

    [Fact]
    public void Archive_ShouldSetIsArchived_AndRaiseEvent()
    {
        var group = BoardGroup.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Group", Color.Create("#000000"), FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)group).ClearDomainEvents();

        group.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        group.IsArchived.Should().BeTrue();
        group.DomainEvents.Should().ContainSingle(e => e is BoardGroupArchivedDomainEvent);
    }

    [Fact]
    public void Archive_ShouldBeIdempotent()
    {
        var group = BoardGroup.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Group", Color.Create("#000000"), FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        group.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)group).ClearDomainEvents();

        group.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        group.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Archive_ShouldThrow_WhenDeleted()
    {
        var group = BoardGroup.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Group", Color.Create("#000000"), FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        group.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        Action act = () => group.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Unarchive_ShouldClearIsArchived_AndRaiseEvent()
    {
        var group = BoardGroup.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Group", Color.Create("#000000"), FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        group.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)group).ClearDomainEvents();

        group.Unarchive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        group.IsArchived.Should().BeFalse();
        group.DomainEvents.Should().ContainSingle(e => e is BoardGroupUnarchivedDomainEvent);
    }

    [Fact]
    public void Unarchive_ShouldBeIdempotent()
    {
        var group = BoardGroup.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Group", Color.Create("#000000"), FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)group).ClearDomainEvents();

        group.Unarchive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        group.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Unarchive_ShouldThrow_WhenDeleted()
    {
        var group = BoardGroup.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Group", Color.Create("#000000"), FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        group.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);
        group.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        Action act = () => group.Unarchive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<DomainException>();
    }
}
