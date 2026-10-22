using FluentAssertions;
using Notrelix.Domain.Collaboration;
using Notrelix.Domain.Collaboration.Attachments;
using Notrelix.Domain.Collaboration.Attachments.Events;

namespace Notrelix.Domain.Tests.Collaboration;

public class AttachmentStoredFileTests
{
    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private static readonly ResourceRef Target =
        ResourceRef.Create(ResourceKind.Create("work-management.board-item"), Guid.NewGuid(), WorkspaceId);

    private static Attachment Create(long size = 1024, long max = 4096, int count = 0, int maxCount = 50, string key = "a/b/attachments/c") =>
        Attachment.CreateStoredFile(
            Guid.NewGuid(), WorkspaceId, Target, AttachmentType.Document, "doc.pdf", size, "application/pdf", key,
            max, count, maxCount, Guid.NewGuid(), DateTimeOffset.UtcNow);

    [Fact]
    public void CreateStoredFile_Succeeds_RaisesCreatedEventOnce_AndIsAStoredFile()
    {
        var attachment = Create();

        attachment.IsStoredFile.Should().BeTrue();
        attachment.Metadata.StorageKey.Should().Be("a/b/attachments/c");
        attachment.Metadata.Url.Should().BeNull();
        attachment.DomainEvents.Should().ContainSingle(e => e is AttachmentCreatedDomainEvent);
    }

    [Fact]
    public void LinkAttachment_IsNotAStoredFile()
    {
        var link = Attachment.Create(Guid.NewGuid(), WorkspaceId, Target, AttachmentType.Link,
            FileMetadata.Create("site", 0, "application/octet-stream", url: "https://example.test"), Guid.NewGuid(), DateTimeOffset.UtcNow);

        link.IsStoredFile.Should().BeFalse();
    }

    [Theory]
    [InlineData(0, CollaborationRuleCodes.Collaboration_Attachment_FileSizeMustBePositive)]
    [InlineData(-5, CollaborationRuleCodes.Collaboration_Attachment_FileSizeMustBePositive)]
    [InlineData(4097, CollaborationRuleCodes.Collaboration_Attachment_FileSizeExceeded)]
    public void CreateStoredFile_RejectsInvalidSize_WithoutCreatingState(long size, string ruleCode)
    {
        var act = () => Create(size: size);

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(ruleCode);
    }

    [Fact]
    public void CreateStoredFile_AtExactSizeCap_Succeeds() => Create(size: 4096).Metadata.Size.Should().Be(4096);

    [Fact]
    public void CreateStoredFile_AtAttachmentLimit_IsRejected()
    {
        var act = () => Create(count: 50, maxCount: 50);

        act.Should().Throw<BusinessRuleException>()
            .Which.RuleCode.Should().Be(CollaborationRuleCodes.Collaboration_Attachment_MaxAttachmentsExceeded);
    }

    [Fact]
    public void CreateStoredFile_RequiresStorageKey_AndRejectsLinkType()
    {
        var noKey = () => Create(key: " ");
        noKey.Should().Throw<BusinessRuleException>();

        var asLink = () => Attachment.CreateStoredFile(
            Guid.NewGuid(), WorkspaceId, Target, AttachmentType.Link, "x", 1, "text/plain", "k", 10, 0, 1, Guid.NewGuid(), DateTimeOffset.UtcNow);
        asLink.Should().Throw<ArgumentException>();
    }
}
