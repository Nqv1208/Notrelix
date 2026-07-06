using FluentAssertions;
using Notrelix.Domain.Collaboration.Attachments;

namespace Notrelix.Domain.Tests.Collaboration;

public class AttachmentTests
{
    [Fact]
    public void Create_ShouldSucceed_AndRaiseEvent()
    {
        var workspaceId = Guid.NewGuid();
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid(), workspaceId);
        var metadata = FileMetadata.Create("doc.pdf", 1024, "application/pdf");

        var attachment = Attachment.Create(Guid.NewGuid(), workspaceId, target, AttachmentType.Document, metadata, Guid.NewGuid(), DateTimeOffset.UtcNow);

        attachment.WorkspaceId.Should().Be(workspaceId);
        attachment.Target.Should().Be(target);
        attachment.Type.Should().Be(AttachmentType.Document);
        attachment.Metadata.Should().Be(metadata);
        attachment.DomainEvents.Should().ContainSingle(e => e is AttachmentCreatedDomainEvent);
    }

    [Fact]
    public void Create_WithWorkspaceMismatch_ShouldThrow()
    {
        var workspaceId = Guid.NewGuid();
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid(), Guid.NewGuid());

        var act = () => Attachment.Create(Guid.NewGuid(), workspaceId, target, AttachmentType.Image, FileMetadata.Create("img.png", 512, "image/png"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<WorkspaceMismatchException>();
    }

    [Fact]
    public void SoftDelete_ShouldSucceed_AndRaiseEvent()
    {
        var attachment = CreateAttachment();
        attachment.ClearDomainEvents();

        attachment.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        attachment.IsDeleted.Should().BeTrue();
        attachment.DomainEvents.Should().ContainSingle(e => e is AttachmentDeletedDomainEvent);
    }

    [Fact]
    public void SoftDelete_WhenAlreadyDeleted_ShouldBeNoOp()
    {
        var attachment = CreateAttachment();
        attachment.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);
        attachment.ClearDomainEvents();

        attachment.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        attachment.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Restore_ShouldSucceed_AndRaiseEvent()
    {
        var attachment = CreateAttachment();
        attachment.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);
        attachment.ClearDomainEvents();

        attachment.Restore(Guid.NewGuid(), DateTimeOffset.UtcNow);

        attachment.IsDeleted.Should().BeFalse();
        attachment.DomainEvents.Should().ContainSingle(e => e is AttachmentRestoredDomainEvent);
    }

    [Fact]
    public void Restore_WhenNotDeleted_ShouldBeNoOp()
    {
        var attachment = CreateAttachment();
        attachment.ClearDomainEvents();

        attachment.Restore(Guid.NewGuid(), DateTimeOffset.UtcNow);

        attachment.DomainEvents.Should().BeEmpty();
    }

    private static Attachment CreateAttachment()
    {
        var workspaceId = Guid.NewGuid();
        return Attachment.Create(Guid.NewGuid(), workspaceId, ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid(), workspaceId), AttachmentType.Document, FileMetadata.Create("doc.pdf", 1024, "application/pdf"), Guid.NewGuid(), DateTimeOffset.UtcNow);
    }
}
