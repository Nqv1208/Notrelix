using FluentAssertions;
using Notrelix.Domain.Tests.Freeze;
using Notrelix.Domain.WorkManagement.BoardGroups;

namespace Notrelix.Domain.Tests.WorkManagement;

[CoversAggregate(typeof(BoardGroup))]
public class BoardGroupTests
{
    [CoversMutation(typeof(BoardGroup), nameof(BoardGroup.UpdateColor), MutationScenario.Event, typeof(Color), typeof(Guid), typeof(DateTimeOffset))]
    [CoversMutation(typeof(BoardGroup), nameof(BoardGroup.UpdatePosition), MutationScenario.Event, typeof(FractionalIndex), typeof(Guid), typeof(DateTimeOffset))]
    [CoversMutation(typeof(BoardGroup), nameof(BoardGroup.Rename), MutationScenario.Event, typeof(string), typeof(Guid), typeof(DateTimeOffset))]
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

    [CoversMutation(typeof(BoardGroup), nameof(BoardGroup.Delete), MutationScenario.Lifecycle, typeof(Guid), typeof(DateTimeOffset), typeof(string))]
    [Fact]
    public void Delete_ShouldRaiseEvent_WithBoardId()
    {
        var accountId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var boardId = Guid.NewGuid();
        var group = BoardGroup.Create(accountId, workspaceId, boardId, "Group", Color.Create("#000000"), FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)group).ClearDomainEvents();

        group.Delete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var deletedEvent = group.DomainEvents.OfType<BoardGroupDeletedDomainEvent>().Single();
        deletedEvent.BoardId.Should().Be(boardId);
        deletedEvent.WorkspaceId.Should().Be(workspaceId);
    }

    [CoversMutation(typeof(BoardGroup), nameof(BoardGroup.Restore), MutationScenario.Lifecycle, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Restore_ShouldClearIsDeleted_AndRaiseEvent()
    {
        var group = BoardGroup.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Group", Color.Create("#000000"), FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        group.Delete(Guid.NewGuid(), DateTimeOffset.UtcNow);
        group.IsDeleted.Should().BeTrue();
        ((IHasDomainEvents)group).ClearDomainEvents();

        group.Restore(Guid.NewGuid(), DateTimeOffset.UtcNow);

        group.IsDeleted.Should().BeFalse();
        group.DomainEvents.Should().ContainSingle(e => e is BoardGroupRestoredDomainEvent);
    }

    [CoversMutation(typeof(BoardGroup), nameof(BoardGroup.Archive), MutationScenario.Event, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Archive_ShouldSetIsArchived_AndRaiseEvent()
    {
        var group = BoardGroup.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Group", Color.Create("#000000"), FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)group).ClearDomainEvents();

        group.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        group.IsArchived.Should().BeTrue();
        group.DomainEvents.Should().ContainSingle(e => e is BoardGroupArchivedDomainEvent);
    }

    [CoversMutation(typeof(BoardGroup), nameof(BoardGroup.Archive), MutationScenario.NoOp, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Archive_ShouldBeIdempotent()
    {
        var group = BoardGroup.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Group", Color.Create("#000000"), FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        group.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)group).ClearDomainEvents();

        group.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        group.DomainEvents.Should().BeEmpty();
    }

    [CoversMutation(typeof(BoardGroup), nameof(BoardGroup.Archive), MutationScenario.Invalid, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Archive_ShouldThrow_WhenDeleted()
    {
        var group = BoardGroup.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Group", Color.Create("#000000"), FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        group.Delete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        Action act = () => group.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<DomainException>();
    }

    [CoversMutation(typeof(BoardGroup), nameof(BoardGroup.Archive), MutationScenario.Event, typeof(Guid), typeof(DateTimeOffset))]
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

    [CoversMutation(typeof(BoardGroup), nameof(BoardGroup.Unarchive), MutationScenario.NoOp, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Unarchive_ShouldBeIdempotent()
    {
        var group = BoardGroup.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Group", Color.Create("#000000"), FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)group).ClearDomainEvents();

        group.Unarchive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        group.DomainEvents.Should().BeEmpty();
    }

    [CoversMutation(typeof(BoardGroup), nameof(BoardGroup.Unarchive), MutationScenario.Invalid, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Unarchive_ShouldThrow_WhenDeleted()
    {
        var group = BoardGroup.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Group", Color.Create("#000000"), FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        group.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);
        group.Delete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        Action act = () => group.Unarchive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<DomainException>();
    }
}
