using FluentAssertions;
using Notrelix.Domain.Collaboration.Activity;
using Notrelix.Domain.Collaboration.Attachments;
using Notrelix.Domain.Collaboration.Comments;
using Notrelix.Domain.Collaboration.Mentions;
using Notrelix.Domain.Collaboration.Notifications;
using Notrelix.Domain.Collaboration.Reactions;
using Notrelix.Domain.Collaboration.Watchers;
using Notrelix.Domain.WorkManagement.Approvals;
using Notrelix.Domain.WorkManagement.Items;

namespace Notrelix.Domain.Tests;

public class Phase2AuditTests
{
    private static readonly Guid WsA = Guid.NewGuid();
    private static readonly Guid WsB = Guid.NewGuid();

    [Fact]
    public void Comment_Create_WithMatchingWorkspace_ShouldSucceed()
    {
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid(), WsA);
        var comment = Comment.Create(WsA, target, "ok", Guid.NewGuid(), DateTimeOffset.UtcNow);
        comment.WorkspaceId.Should().Be(WsA);
    }

    [Fact]
    public void Comment_Create_WithMismatchedWorkspace_ShouldThrow()
    {
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid(), WsB);
        var act = () => Comment.Create(WsA, target, "bad", Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<WorkspaceMismatchException>();
    }

    [Fact]
    public void Comment_Create_WithUnscopedResourceRef_ShouldSucceed()
    {
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid());
        var comment = Comment.Create(WsA, target, "ok", Guid.NewGuid(), DateTimeOffset.UtcNow);
        comment.WorkspaceId.Should().Be(WsA);
    }

    [Fact]
    public void Attachment_Create_WithMatchingWorkspace_ShouldSucceed()
    {
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid(), WsA);
        var meta = FileMetadata.Create("f.pdf", 100, "application/pdf");
        var attachment = Attachment.Create(WsA, target, AttachmentType.Document, meta, Guid.NewGuid(), DateTimeOffset.UtcNow);
        attachment.WorkspaceId.Should().Be(WsA);
    }

    [Fact]
    public void Attachment_Create_WithMismatchedWorkspace_ShouldThrow()
    {
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid(), WsB);
        var meta = FileMetadata.Create("f.pdf", 100, "application/pdf");
        var act = () => Attachment.Create(WsA, target, AttachmentType.Document, meta, Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<WorkspaceMismatchException>();
    }

    [Fact]
    public void Attachment_Create_WithUnscopedResourceRef_ShouldSucceed()
    {
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid());
        var meta = FileMetadata.Create("f.pdf", 100, "application/pdf");
        var attachment = Attachment.Create(WsA, target, AttachmentType.Document, meta, Guid.NewGuid(), DateTimeOffset.UtcNow);
        attachment.WorkspaceId.Should().Be(WsA);
    }

    [Fact]
    public void ActivityLog_Record_WithMatchingWorkspace_ShouldSucceed()
    {
        var target = ResourceRef.Create(ResourceType.Board, Guid.NewGuid(), WsA);
        var log = ActivityLog.Record(WsA, Guid.NewGuid(), ActivityType.Created, target, DateTimeOffset.UtcNow);
        log.WorkspaceId.Should().Be(WsA);
    }

    [Fact]
    public void ActivityLog_Record_WithMismatchedWorkspace_ShouldThrow()
    {
        var target = ResourceRef.Create(ResourceType.Board, Guid.NewGuid(), WsB);
        var act = () => ActivityLog.Record(WsA, Guid.NewGuid(), ActivityType.Created, target, DateTimeOffset.UtcNow);
        act.Should().Throw<WorkspaceMismatchException>();
    }

    [Fact]
    public void ActivityLog_Record_WithUnscopedResourceRef_ShouldSucceed()
    {
        var target = ResourceRef.Create(ResourceType.Board, Guid.NewGuid());
        var log = ActivityLog.Record(WsA, Guid.NewGuid(), ActivityType.Created, target, DateTimeOffset.UtcNow);
        log.WorkspaceId.Should().Be(WsA);
    }

    [Fact]
    public void ResourceWatcher_Create_WithMatchingWorkspace_ShouldSucceed()
    {
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid(), WsA);
        var watcher = ResourceWatcher.Create(WsA, target, Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        watcher.WorkspaceId.Should().Be(WsA);
    }

    [Fact]
    public void ResourceWatcher_Create_WithMismatchedWorkspace_ShouldThrow()
    {
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid(), WsB);
        var act = () => ResourceWatcher.Create(WsA, target, Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<WorkspaceMismatchException>();
    }

    [Fact]
    public void ResourceWatcher_Create_WithUnscopedResourceRef_ShouldSucceed()
    {
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid());
        var watcher = ResourceWatcher.Create(WsA, target, Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        watcher.WorkspaceId.Should().Be(WsA);
    }

    [Fact]
    public void Reaction_Create_WithMatchingWorkspace_ShouldSucceed()
    {
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid(), WsA);
        var reaction = Reaction.Create(WsA, target, Guid.NewGuid(), Emoji.Create("+1"), DateTimeOffset.UtcNow);
        reaction.WorkspaceId.Should().Be(WsA);
    }

    [Fact]
    public void Reaction_Create_WithMismatchedWorkspace_ShouldThrow()
    {
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid(), WsB);
        var act = () => Reaction.Create(WsA, target, Guid.NewGuid(), Emoji.Create("+1"), DateTimeOffset.UtcNow);
        act.Should().Throw<WorkspaceMismatchException>();
    }

    [Fact]
    public void Reaction_Create_WithUnscopedResourceRef_ShouldSucceed()
    {
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid());
        var reaction = Reaction.Create(WsA, target, Guid.NewGuid(), Emoji.Create("+1"), DateTimeOffset.UtcNow);
        reaction.WorkspaceId.Should().Be(WsA);
    }

    [Fact]
    public void Mention_Create_WithMatchingWorkspace_ShouldSucceed()
    {
        var source = ResourceRef.Create(ResourceType.Comment, Guid.NewGuid(), WsA);
        var mention = Mention.Create(WsA, source, MentionType.User, Guid.NewGuid(), DateTimeOffset.UtcNow);
        mention.WorkspaceId.Should().Be(WsA);
    }

    [Fact]
    public void Mention_Create_WithMismatchedWorkspace_ShouldThrow()
    {
        var source = ResourceRef.Create(ResourceType.Comment, Guid.NewGuid(), WsB);
        var act = () => Mention.Create(WsA, source, MentionType.User, Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<WorkspaceMismatchException>();
    }

    [Fact]
    public void Mention_Create_WithUnscopedResourceRef_ShouldSucceed()
    {
        var source = ResourceRef.Create(ResourceType.Comment, Guid.NewGuid());
        var mention = Mention.Create(WsA, source, MentionType.User, Guid.NewGuid(), DateTimeOffset.UtcNow);
        mention.WorkspaceId.Should().Be(WsA);
    }

    [Fact]
    public void Notification_Create_WithMatchingWorkspace_ShouldSucceed()
    {
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid(), WsA);
        var notification = Notification.Create(Guid.NewGuid(), WsA, NotificationType.System, "Title", "Content", DateTimeOffset.UtcNow, target);
        notification.WorkspaceId.Should().Be(WsA);
    }

    [Fact]
    public void Notification_Create_WithMismatchedWorkspace_ShouldThrow()
    {
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid(), WsB);
        var act = () => Notification.Create(Guid.NewGuid(), WsA, NotificationType.System, "Title", "Content", DateTimeOffset.UtcNow, target);
        act.Should().Throw<WorkspaceMismatchException>();
    }

    [Fact]
    public void Notification_Create_WithUnscopedResourceRef_ShouldSucceed()
    {
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid());
        var notification = Notification.Create(Guid.NewGuid(), WsA, NotificationType.System, "Title", "Content", DateTimeOffset.UtcNow, target);
        notification.WorkspaceId.Should().Be(WsA);
    }

    [Fact]
    public void Notification_Create_WithNullTarget_ShouldSucceed()
    {
        var notification = Notification.Create(Guid.NewGuid(), WsA, NotificationType.System, "Title", "Content", DateTimeOffset.UtcNow);
        notification.WorkspaceId.Should().Be(WsA);
    }

    [Fact]
    public void ApprovalRequest_Create_WithMatchingWorkspace_ShouldSucceed()
    {
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid(), WsA);
        var request = ApprovalRequest.Create(WsA, target, "Approve", Guid.NewGuid(), DateTimeOffset.UtcNow);
        request.WorkspaceId.Should().Be(WsA);
    }

    [Fact]
    public void ApprovalRequest_Create_WithMismatchedWorkspace_ShouldThrow()
    {
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid(), WsB);
        var act = () => ApprovalRequest.Create(WsA, target, "Approve", Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<WorkspaceMismatchException>();
    }

    [Fact]
    public void ApprovalRequest_Create_WithUnscopedResourceRef_ShouldSucceed()
    {
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid());
        var request = ApprovalRequest.Create(WsA, target, "Approve", Guid.NewGuid(), DateTimeOffset.UtcNow);
        request.WorkspaceId.Should().Be(WsA);
    }

    [Fact]
    public void BoardItemLink_Create_WithMatchingWorkspace_ShouldSucceed()
    {
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid(), WsA);
        var link = BoardItemLink.Create(WsA, Guid.NewGuid(), Guid.NewGuid(), target, BoardItemLinkType.Reference, null, DateTimeOffset.UtcNow);
        link.WorkspaceId.Should().Be(WsA);
    }

    [Fact]
    public void BoardItemLink_Create_WithMismatchedWorkspace_ShouldThrow()
    {
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid(), WsB);
        var act = () => BoardItemLink.Create(WsA, Guid.NewGuid(), Guid.NewGuid(), target, BoardItemLinkType.Reference, null, DateTimeOffset.UtcNow);
        act.Should().Throw<WorkspaceMismatchException>();
    }

    [Fact]
    public void BoardItemLink_Create_WithUnscopedResourceRef_ShouldSucceed()
    {
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid());
        var link = BoardItemLink.Create(WsA, Guid.NewGuid(), Guid.NewGuid(), target, BoardItemLinkType.Reference, null, DateTimeOffset.UtcNow);
        link.WorkspaceId.Should().Be(WsA);
    }
}
