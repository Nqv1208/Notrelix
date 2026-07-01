using FluentAssertions;
using Notrelix.Domain.Collaboration.Attachments;

namespace Notrelix.Domain.Tests.Collaboration;

public class AttachmentWorkspaceScopeTests
{
    private static readonly Guid WsA = Guid.NewGuid();
    private static readonly Guid WsB = Guid.NewGuid();

    [Fact]
    public void Create_WithMatchingWorkspace_ShouldSucceed()
    {
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid(), WsA);
        var meta = FileMetadata.Create("f.pdf", 100, "application/pdf");
        var attachment = Attachment.Create(Guid.NewGuid(), WsA, target, AttachmentType.Document, meta, Guid.NewGuid(), DateTimeOffset.UtcNow);
        attachment.WorkspaceId.Should().Be(WsA);
    }

    [Fact]
    public void Create_WithMismatchedWorkspace_ShouldThrow()
    {
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid(), WsB);
        var meta = FileMetadata.Create("f.pdf", 100, "application/pdf");
        var act = () => Attachment.Create(Guid.NewGuid(), WsA, target, AttachmentType.Document, meta, Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<WorkspaceMismatchException>();
    }

    [Fact]
    public void Create_WithUnscopedResourceRef_ShouldSucceed()
    {
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid());
        var meta = FileMetadata.Create("f.pdf", 100, "application/pdf");
        var attachment = Attachment.Create(Guid.NewGuid(), WsA, target, AttachmentType.Document, meta, Guid.NewGuid(), DateTimeOffset.UtcNow);
        attachment.WorkspaceId.Should().Be(WsA);
    }
}
