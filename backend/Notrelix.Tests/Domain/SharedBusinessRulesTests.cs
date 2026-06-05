using Notrelix.Domain.Entities.Shared;
using Notrelix.Domain.Enums;
using Notrelix.Domain.Events.Document;
using Notrelix.Domain.Events.Shared;

namespace Notrelix.Domain.Tests;

public class SharedBusinessRulesTests
{
    [Fact]
    public void CommentLifecycle_ShouldRaiseCommentEvents()
    {
        var workspaceId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var comment = Comment.Create(workspaceId, ResourceType.Card, resourceId, userId, "Initial update");

        comment.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<CommentCreatedEvent>();

        comment.ClearDomainEvents();
        comment.Edit("Edited update");
        comment.SoftDelete();

        comment.ContentMd.Should().Be("[Đã xóa]");
        comment.IsDeleted.Should().BeTrue();
        comment.DomainEvents.Select(e => e.GetType()).Should().ContainInOrder(
            typeof(CommentUpdatedEvent),
            typeof(CommentDeletedEvent));
    }

    [Fact]
    public void PageMentionCreate_ShouldRaisePageMentionedEvent()
    {
        var pageId = Guid.NewGuid();
        var blockId = Guid.NewGuid();
        var mentionedUserId = Guid.NewGuid();
        var mentionedBy = Guid.NewGuid();

        var mention = PageMention.Create(pageId, mentionedUserId, mentionedBy, blockId);

        var domainEvent = mention.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<PageMentionedEvent>().Subject;
        domainEvent.PageId.Should().Be(pageId);
        domainEvent.BlockId.Should().Be(blockId);
        domainEvent.MentionedUserId.Should().Be(mentionedUserId);
        domainEvent.MentionedBy.Should().Be(mentionedBy);
    }
}
