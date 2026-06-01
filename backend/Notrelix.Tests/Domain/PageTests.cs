using Notrelix.Domain.Entities.Document;
using Notrelix.Domain.Events.Document;

namespace Notrelix.Domain.Tests;

public class PageTests
{
    [Fact]
    public void Publish_ShouldRegisterPagePublishedEvent()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();
        var page = Page.Create(workspaceId, creatorId, "Intro to DDD");

        // Act
        page.Publish(creatorId);

        // Assert
        page.PublishedAt.Should().NotBeNull();
        page.DomainEvents.Should().HaveCount(1);
        
        var domainEvent = page.DomainEvents.First().Should().BeOfType<PagePublishedEvent>().Subject;
        domainEvent.PageId.Should().Be(page.Id);
        domainEvent.WorkspaceId.Should().Be(workspaceId);
        domainEvent.PublishedBy.Should().Be(creatorId);
    }

    [Fact]
    public void SetDeadline_WhenChanged_ShouldRegisterPageDeadlineSetEvent()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();
        var page = Page.Create(workspaceId, creatorId, "Milestone doc");
        var deadline = DateTime.UtcNow.AddDays(7);

        // Act
        page.SetDeadline(deadline);

        // Assert
        page.Deadline.Should().Be(deadline);
        page.DomainEvents.Should().HaveCount(1);

        var domainEvent = page.DomainEvents.First().Should().BeOfType<PageDeadlineSetEvent>().Subject;
        domainEvent.PageId.Should().Be(page.Id);
        domainEvent.WorkspaceId.Should().Be(workspaceId);
        domainEvent.Deadline.Should().Be(deadline);
    }

    [Fact]
    public void BlockMove_WhenSelfParenting_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var pageId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var block = Block.Create(pageId, userId, "paragraph");

        // Act
        var act = () => block.Move(1.5, block.Id);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("A block cannot be its own parent.");
    }
}
