using FluentAssertions;
using Notrelix.Domain.Tests.Freeze;
using Notrelix.Domain.WorkManagement.BoardGroups;

namespace Notrelix.Domain.Tests.WorkManagement;

[CoversAggregate(typeof(BoardGroup))]
public class BoardGroupTests
{
    [CoversMutation(typeof(BoardGroup), "UpdateColor(Notrelix.Domain.SharedKernel.Color,System.Guid,System.DateTimeOffset)", MutationScenario.Event)]
    [CoversMutation(typeof(BoardGroup), "UpdatePosition(Notrelix.Domain.SharedKernel.Ordering.FractionalIndex,System.Guid,System.DateTimeOffset)", MutationScenario.Event)]
    [CoversMutation(typeof(BoardGroup), "Rename(System.String,System.Guid,System.DateTimeOffset)", MutationScenario.Event)]
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

    [CoversMutation(typeof(BoardGroup), "Delete(System.Guid,System.DateTimeOffset,System.String)", MutationScenario.Lifecycle)]
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

    [CoversMutation(typeof(BoardGroup), "Restore(System.Guid,System.DateTimeOffset)", MutationScenario.Lifecycle)]
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

    [CoversMutation(typeof(BoardGroup), "Archive(System.Guid,System.DateTimeOffset)", MutationScenario.Event)]
    [Fact]
    public void Archive_ShouldSetIsArchived_AndRaiseEvent()
    {
        var group = BoardGroup.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Group", Color.Create("#000000"), FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)group).ClearDomainEvents();

        group.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        group.IsArchived.Should().BeTrue();
        group.DomainEvents.Should().ContainSingle(e => e is BoardGroupArchivedDomainEvent);
    }

    [CoversMutation(typeof(BoardGroup), "Archive(System.Guid,System.DateTimeOffset)", MutationScenario.NoOp)]
    [Fact]
    public void Archive_ShouldBeIdempotent()
    {
        var group = BoardGroup.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Group", Color.Create("#000000"), FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        group.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)group).ClearDomainEvents();

        group.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        group.DomainEvents.Should().BeEmpty();
    }

    [CoversMutation(typeof(BoardGroup), "Archive(System.Guid,System.DateTimeOffset)", MutationScenario.Invalid)]
    [Fact]
    public void Archive_ShouldThrow_WhenDeleted()
    {
        var group = BoardGroup.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Group", Color.Create("#000000"), FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        group.Delete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        Action act = () => group.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<DomainException>();
    }

    [CoversMutation(typeof(BoardGroup), "Archive(System.Guid,System.DateTimeOffset)", MutationScenario.Event)]
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

    [CoversMutation(typeof(BoardGroup), "Unarchive(System.Guid,System.DateTimeOffset)", MutationScenario.NoOp)]
    [Fact]
    public void Unarchive_ShouldBeIdempotent()
    {
        var group = BoardGroup.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Group", Color.Create("#000000"), FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)group).ClearDomainEvents();

        group.Unarchive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        group.DomainEvents.Should().BeEmpty();
    }

    [CoversMutation(typeof(BoardGroup), "Unarchive(System.Guid,System.DateTimeOffset)", MutationScenario.Invalid)]
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
