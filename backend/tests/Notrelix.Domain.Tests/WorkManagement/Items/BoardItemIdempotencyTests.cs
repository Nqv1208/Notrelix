using FluentAssertions;
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Domain.WorkManagement.BoardGroups;

namespace Notrelix.Domain.Tests.WorkManagement;

public class BoardItemIdempotencyTests
{
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _boardId = Guid.NewGuid();
    private readonly Guid _actorId = Guid.NewGuid();
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    [Fact]
    public void Rename_ShouldNotIncrementVersion_WhenNameIsSame()
    {
        var position = FractionalIndex.Create("a0");
        var item = BoardItem.Create(_workspaceId, _boardId, Guid.NewGuid(), "Item", position, _actorId, _now);
        var version = item.Version;

        item.Rename("Item", _actorId, _now);

        item.Version.Should().Be(version);
        item.DomainEvents.Should().NotContain(e => e is BoardItemRenamedDomainEvent);
    }

    [Fact]
    public void MoveToGroup_ShouldNotIncrementVersion_WhenGroupAndPositionAreSame()
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
    public void SoftDelete_ShouldIncrementVersion_AndRaiseEvent()
    {
        var item = BoardItem.Create(_workspaceId, _boardId, Guid.NewGuid(), "Item", FractionalIndex.Create("a0"), _actorId, _now);
        var version = item.Version;

        item.SoftDelete(_actorId, _now);

        item.IsDeleted.Should().BeTrue();
        item.Version.Should().Be(version + 1);
        item.DomainEvents.Should().ContainSingle(e => e is BoardItemSoftDeletedDomainEvent);
    }
}
