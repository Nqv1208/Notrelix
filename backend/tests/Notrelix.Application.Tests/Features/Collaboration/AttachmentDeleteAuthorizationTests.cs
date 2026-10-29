using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Collaboration.Abstractions;
using Notrelix.Application.Features.Collaboration.Attachments.Commands.DeleteAttachment;
using Notrelix.Application.Features.Collaboration.Attachments.Commands.DeletePageAttachment;
using Notrelix.Application.Features.Collaboration.Attachments.Services;
using Notrelix.Application.Features.Collaboration.Comments.Services;
using Notrelix.Domain.Collaboration.Attachments;

namespace Notrelix.Application.Tests.Features.Collaboration;

/// <summary>Deleting an attachment is for its uploader or an editor of the target; no view access is NotFound.</summary>
public class AttachmentDeleteAuthorizationTests
{
    private static readonly DateTimeOffset Now = new(2025, 1, 15, 10, 0, 0, TimeSpan.Zero);

    private readonly Guid _uploader = Guid.CreateVersion7();
    private readonly Guid _other = Guid.CreateVersion7();
    private readonly Guid _account = Guid.CreateVersion7();
    private readonly Guid _workspace = Guid.CreateVersion7();
    private readonly List<Attachment> _rows = [];
    private readonly Mock<ICollaborationDbContext> _context = new();
    private readonly Mock<ICurrentUser> _user = new();
    private readonly Mock<IDateTimeProvider> _clock = new();
    private readonly Mock<ICommentTargetAccess> _access = new();
    private bool _canView = true;
    private bool _canEdit;

    public AttachmentDeleteAuthorizationTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(Now);
        _context.Setup(c => c.Attachments).Returns(TestDbSet.Create(_rows).Object);
        _access.Setup(a => a.HasAsync(It.IsAny<ResourceRef>(), It.IsAny<CommentTargetRight>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ResourceRef _, CommentTargetRight right, CancellationToken _) => right == CommentTargetRight.View ? _canView : _canEdit);
    }

    private Attachment Link(ResourceKind kind, Guid targetId)
    {
        var target = ResourceRef.Create(kind, targetId, _workspace);
        var row = Attachment.Create(_account, _workspace, target, AttachmentType.Link, FileMetadata.Create("a", 0, "text/uri-list", url: "https://example.com"), _uploader, Now);
        _rows.Add(row);
        return row;
    }

    private Task<Result> Delete(bool page, Attachment row, Guid actor)
    {
        _user.Setup(u => u.UserId).Returns(actor);
        return page
            ? new DeletePageAttachmentCommandHandler(_context.Object, _user.Object, _clock.Object, _access.Object)
                .Handle(new DeletePageAttachmentCommand(row.Target.ResourceId, row.Id), default)
            : new DeleteAttachmentCommandHandler(_context.Object, _user.Object, _clock.Object, _access.Object)
                .Handle(new DeleteAttachmentCommand(row.Id), default);
    }

    private Attachment Row(bool page) => page
        ? Link(AttachmentTargets.Page, Guid.CreateVersion7())
        : Link(AttachmentTargets.BoardItem, Guid.CreateVersion7());

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Uploader_WithoutEditRights_MayDelete(bool page)
    {
        var row = Row(page);

        (await Delete(page, row, _uploader)).Succeeded.Should().BeTrue();

        row.IsDeleted.Should().BeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EditorOfTheTarget_MayDeleteAnotherPersonsAttachment_IncludingTheWorkspaceOwner(bool page)
    {
        // The Owner holds edit rights on every target, so the target access reports Edit for them.
        _canEdit = true;
        var row = Row(page);

        (await Delete(page, row, _other)).Succeeded.Should().BeTrue();

        row.IsDeleted.Should().BeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OtherMemberWhoCanOnlyView_IsForbiddenWithAStableCode(bool page)
    {
        var row = Row(page);

        var act = () => Delete(page, row, _other);

        (await act.Should().ThrowAsync<ForbiddenException>()).Which.Code.Should().Be(AttachmentAccessRules.UploaderOrEditorCode);
        row.IsDeleted.Should().BeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NoViewAccess_IsNotFound_EvenForTheUploader(bool page)
    {
        _canView = false;
        var row = Row(page);

        var act = () => Delete(page, row, _uploader);

        await act.Should().ThrowAsync<NotFoundException>();
        row.IsDeleted.Should().BeFalse();
    }
}
