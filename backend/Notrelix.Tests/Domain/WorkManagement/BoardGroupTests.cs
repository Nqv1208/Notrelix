using FluentAssertions;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.WorkManagement.BoardGroups;
using Xunit;

namespace Notrelix.Domain.Tests.WorkManagement;

public class BoardGroupTests
{
    [Fact]
    public void UpdateColor_ShouldRaiseColorChangedEvent()
    {
        var workspaceId = Guid.NewGuid();
        var boardId = Guid.NewGuid();
        var group = BoardGroup.Create(workspaceId, boardId, "Group", Color.Create("#000000"), FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        group.ClearDomainEvents();

        group.UpdateColor(Color.Create("#FF0000"), Guid.NewGuid(), DateTimeOffset.UtcNow);

        group.Color.Value.Should().Be("#FF0000");
        group.DomainEvents.Should().ContainSingle(e => e is BoardGroupColorChangedEvent);
    }

    [Fact]
    public void SoftDelete_ShouldRaiseEvent_WithBoardId()
    {
        var workspaceId = Guid.NewGuid();
        var boardId = Guid.NewGuid();
        var group = BoardGroup.Create(workspaceId, boardId, "Group", Color.Create("#000000"), FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        group.ClearDomainEvents();

        group.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var deletedEvent = group.DomainEvents.OfType<BoardGroupSoftDeletedEvent>().Single();
        deletedEvent.BoardId.Should().Be(boardId);
        deletedEvent.WorkspaceId.Should().Be(workspaceId);
    }

    [Fact]
    public void Restore_ShouldClearIsDeleted_AndRaiseEvent()
    {
        var group = BoardGroup.Create(Guid.NewGuid(), Guid.NewGuid(), "Group", Color.Create("#000000"), FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        group.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);
        group.IsDeleted.Should().BeTrue();
        group.ClearDomainEvents();

        group.Restore(Guid.NewGuid(), DateTimeOffset.UtcNow);

        group.IsDeleted.Should().BeFalse();
        group.DomainEvents.Should().ContainSingle(e => e is BoardGroupRestoredEvent);
    }
}
