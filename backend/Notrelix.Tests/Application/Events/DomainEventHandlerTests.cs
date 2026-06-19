using MediatR;
using Notrelix.Application.Common.Events;
using Notrelix.Domain.Common;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement.Boards.Events;
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Domain.WorkManagement.Items.Events;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Application.Tests.Events;

public class DomainEventHandlerTests
{
    [Fact]
    public void BoardCreatedDomainEvent_ShouldBeWrappedInDomainEventNotification()
    {
        var workspaceId = Guid.NewGuid();
        var boardId = Guid.NewGuid();
        var createdBy = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var domainEvent = new BoardCreatedDomainEvent(workspaceId, boardId, "Roadmap", createdBy, now);
        var notification = new DomainEventNotification<BoardCreatedDomainEvent>(domainEvent);

        notification.DomainEvent.Should().Be(domainEvent);
        notification.DomainEvent.WorkspaceId.Should().Be(workspaceId);
    }

    [Fact]
    public void BoardItemMemberAssignedDomainEvent_ShouldCarryCorrectData()
    {
        var workspaceId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var assignedBy = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var domainEvent = new BoardItemMemberAssignedDomainEvent(
            workspaceId, itemId, userId, assignedBy, now);

        domainEvent.WorkspaceId.Should().Be(workspaceId);
        domainEvent.ItemId.Should().Be(itemId);
        domainEvent.UserId.Should().Be(userId);
        domainEvent.AssignedBy.Should().Be(assignedBy);
    }

    [Fact]
    public void BoardCreate_ShouldRaiseBoardCreatedDomainEvent()
    {
        var workspaceId = Guid.NewGuid();
        var createdBy = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var board = Board.Create(workspaceId, createdBy, "Test Board", null, now);

        var domainEvents = board.DomainEvents;
        domainEvents.Should().ContainSingle(e => e is BoardCreatedDomainEvent);
        var raisedEvent = domainEvents.OfType<BoardCreatedDomainEvent>().Single();
        raisedEvent.BoardId.Should().Be(board.Id);
        raisedEvent.Title.Should().Be("Test Board");
    }

    [Fact]
    public void BoardItemCreate_ShouldRaiseBoardItemCreatedDomainEvent()
    {
        var workspaceId = Guid.NewGuid();
        var boardId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var createdBy = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var position = FractionalIndex.Initial();

        var item = BoardItem.Create(workspaceId, boardId, groupId, "Task", position, createdBy, now);

        var domainEvents = item.DomainEvents;
        domainEvents.Should().ContainSingle(e => e is BoardItemCreatedDomainEvent);
        var raisedEvent = domainEvents.OfType<BoardItemCreatedDomainEvent>().Single();
        raisedEvent.ItemId.Should().Be(item.Id);
        raisedEvent.Name.Should().Be("Task");
    }

    [Fact]
    public void ResourceType_ShouldContainExpectedTypes()
    {
        ResourceType.Board.Should().Be(ResourceType.Board);
        ResourceType.BoardItem.Should().Be(ResourceType.BoardItem);
        ResourceType.Workspace.Should().Be(ResourceType.Workspace);
    }
}
