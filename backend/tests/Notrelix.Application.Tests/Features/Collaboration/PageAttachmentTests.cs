using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Notrelix.Application.Common.Storage;
using Notrelix.Application.Features.Collaboration.Abstractions;
using Notrelix.Application.Features.Collaboration.Attachments;
using Notrelix.Application.Features.Collaboration.Attachments.Commands.CreateBoardItemAttachment;
using Notrelix.Application.Features.Collaboration.Attachments.Commands.CreatePageAttachment;
using Notrelix.Application.Features.Collaboration.Attachments.Commands.DeleteAttachment;
using Notrelix.Application.Features.Collaboration.Attachments.Commands.DeletePageAttachment;
using Notrelix.Application.Features.Collaboration.Attachments.Commands.UploadPageAttachment;
using Notrelix.Application.Features.Collaboration.Attachments.Queries.GetBoardItemAttachmentContent;
using Notrelix.Application.Features.Collaboration.Attachments.Queries.GetPageAttachmentContent;
using Notrelix.Application.Features.Collaboration.Attachments.Queries.GetPageAttachments;
using Notrelix.Application.Features.Collaboration.Attachments.Services;
using Notrelix.Domain.Collaboration.Attachments;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Application.Tests.Features.Collaboration;

/// <summary>
/// The Documents page target of Collaboration attachments (NRX-003/004): page view to list and
/// read, page edit to register, upload and delete, and every row access scoped to the exact
/// target so an id of another target is never reachable through a page route.
/// </summary>
public sealed class PageAttachmentTests
{
    private static readonly Guid AccountId = Guid.NewGuid();
    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private static readonly Guid PageId = Guid.NewGuid();
    private static readonly Guid OtherPageId = Guid.NewGuid();
    private static readonly Guid ItemId = Guid.NewGuid();
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3, 4];

    private readonly List<Attachment> _rows = [];
    private readonly Dictionary<string, byte[]> _objects = new();
    private readonly FakeCurrentRequestContext _request = new() { UserId = Guid.NewGuid() };

    public PageAttachmentTests() => _request.Tenant.SetWorkspace(AccountId, WorkspaceId, _request.UserId);

    private sealed class MemoryStorage(Dictionary<string, byte[]> objects) : IStorageService
    {
        public async Task WriteAsync(string key, Stream content, string contentType, CancellationToken ct)
        {
            using var buffer = new MemoryStream();
            await content.CopyToAsync(buffer, ct);
            objects[key] = buffer.ToArray();
        }

        public Task<Stream?> OpenReadAsync(string key, CancellationToken ct) =>
            Task.FromResult<Stream?>(objects.TryGetValue(key, out var bytes) ? new MemoryStream(bytes) : null);

        public Task DeleteAsync(string key, CancellationToken ct)
        {
            objects.Remove(key);
            return Task.CompletedTask;
        }

        public async IAsyncEnumerable<StorageObjectInfo> ListAsync(
            string prefix, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
        {
            await Task.CompletedTask;
            yield break;
        }
    }

    private sealed class Clock : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }

    private sealed class NoActors : IActorLookupService
    {
        public Task<IReadOnlyList<ActorSnapshot>> FindManyAsync(IReadOnlyList<Guid> userIds, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<ActorSnapshot>>([]);

        public Task<ActorSnapshot?> FindAsync(Guid userId, CancellationToken ct) => Task.FromResult<ActorSnapshot?>(null);
    }

    private ICurrentUser CurrentUser()
    {
        var user = new Mock<ICurrentUser>();
        user.SetupGet(u => u.UserId).Returns(_request.UserId);
        return user.Object;
    }

    private ICollaborationDbContext Db()
    {
        var db = new Mock<ICollaborationDbContext>();
        db.Setup(c => c.Attachments).Returns(TestDbSet.Create(_rows).Object);
        return db.Object;
    }

    private UploadPageAttachmentCommandHandler UploadHandler(AttachmentUploadOptions? options = null) =>
        new(Db(), _request, new Clock(), new MemoryStorage(_objects),
            new RequestRollbackCompensations(NullLogger<RequestRollbackCompensations>.Instance),
            new InMemoryAttachmentTargetLock(), Options.Create(options ?? new AttachmentUploadOptions()));

    private async Task<Attachment> UploadAsync(Guid page, string name = "photo.png")
    {
        var result = await UploadHandler().Handle(new UploadPageAttachmentCommand(page, name, new MemoryStream(Png)), default);
        result.Succeeded.Should().BeTrue();
        return _rows.Single(r => r.Id == result.Data!.Id);
    }

    [Fact]
    public void PageUseCases_RequireViewToReadAndEditToWrite_OnTheDocumentsPage()
    {
        var attachment = Guid.NewGuid();

        new GetPageAttachmentsQuery(PageId).Action.Should().Be(PermissionAction.ViewPage);
        new GetPageAttachmentContentQuery(PageId, attachment).Action.Should().Be(PermissionAction.ViewPage);
        new UploadPageAttachmentCommand(PageId, "a.png", Stream.Null).Action.Should().Be(PermissionAction.UpdatePage);
        new CreatePageAttachmentCommand(PageId, "a", "https://example.com", null, null).Action.Should().Be(PermissionAction.UpdatePage);
        new DeletePageAttachmentCommand(PageId, attachment).Action.Should().Be(PermissionAction.UpdatePage);

        foreach (var resource in new[]
        {
            new GetPageAttachmentsQuery(PageId).Resource,
            new GetPageAttachmentContentQuery(PageId, attachment).Resource,
            new UploadPageAttachmentCommand(PageId, "a.png", Stream.Null).Resource,
            new DeletePageAttachmentCommand(PageId, attachment).Resource,
        })
        {
            resource.Kind.Value.Should().Be("documents.page");
            resource.ResourceId.Should().Be(PageId);
        }
    }

    [Fact]
    public async Task Upload_StoresTheFileUnderThePageTarget_WithTheSharedFilePolicy()
    {
        var row = await UploadAsync(PageId);

        row.Target.Kind.Value.Should().Be("documents.page");
        row.Target.ResourceId.Should().Be(PageId);
        row.Metadata.StorageKey.Should().StartWith($"{AccountId:N}/{WorkspaceId:N}/attachments/");
        row.Metadata.ContentType.Should().Be("image/png");

        var blocked = () => UploadHandler().Handle(new UploadPageAttachmentCommand(PageId, "run.exe", new MemoryStream(Png)), default);
        await blocked.Should().ThrowAsync<UnsupportedMediaTypeException>();
        _rows.Should().HaveCount(1);
    }

    [Fact]
    public async Task Upload_UsesThePerPageLimit_NotThePerItemLimit()
    {
        var options = new AttachmentUploadOptions { MaxAttachmentsPerItem = 1, MaxAttachmentsPerPage = 2 };
        var handler = UploadHandler(options);
        await handler.Handle(new UploadPageAttachmentCommand(PageId, "a.png", new MemoryStream(Png)), default);
        await handler.Handle(new UploadPageAttachmentCommand(PageId, "b.png", new MemoryStream(Png)), default);

        var third = () => handler.Handle(new UploadPageAttachmentCommand(PageId, "c.png", new MemoryStream(Png)), default);

        await third.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>();
        _rows.Should().HaveCount(2);
    }

    [Fact]
    public async Task List_ReturnsOnlyThisPagesAttachments()
    {
        await UploadAsync(PageId);
        await UploadAsync(OtherPageId);
        var handler = new GetPageAttachmentsQueryHandler(Db(), new NoActors());

        var result = await handler.Handle(new GetPageAttachmentsQuery(PageId), default);

        result.Data!.Should().ContainSingle().Which.ResourceId.Should().Be(PageId);
    }

    private GetPageAttachmentContentQueryHandler ContentHandler() =>
        new(Db(), _request, new MemoryStorage(_objects), NullLogger<GetPageAttachmentContentQueryHandler>.Instance);

    [Fact]
    public async Task Content_OfThisPagesAttachment_IsReturned()
    {
        var row = await UploadAsync(PageId);

        var result = await ContentHandler().Handle(new GetPageAttachmentContentQuery(PageId, row.Id), default);

        result.Data!.ContentType.Should().Be("image/png");
        using var stream = result.Data.Content;
        using var copy = new MemoryStream();
        await stream.CopyToAsync(copy);
        copy.ToArray().Should().Equal(Png);
    }

    [Fact]
    public async Task Content_OfAnotherPagesOrABoardItemsAttachment_IsNotFound()
    {
        var otherPage = await UploadAsync(OtherPageId);
        var itemRow = Attachment.CreateStoredFile(
            AccountId, WorkspaceId, ResourceRef.Create(AttachmentTargets.BoardItem, ItemId, WorkspaceId),
            AttachmentType.Image, "item.png", Png.Length, "image/png",
            $"{AccountId:N}/{WorkspaceId:N}/attachments/{Guid.NewGuid():N}", 1024 * 1024, 0, 50, _request.UserId, DateTimeOffset.UtcNow);
        _rows.Add(itemRow);
        _objects[itemRow.Metadata.StorageKey!] = Png;

        var foreignPage = () => ContentHandler().Handle(new GetPageAttachmentContentQuery(PageId, otherPage.Id), default);
        var boardItem = () => ContentHandler().Handle(new GetPageAttachmentContentQuery(PageId, itemRow.Id), default);

        await foreignPage.Should().ThrowAsync<NotFoundException>();
        await boardItem.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task BoardItemContentRoute_DoesNotServeAPageAttachment()
    {
        var row = await UploadAsync(PageId);
        var handler = new GetBoardItemAttachmentContentQueryHandler(
            Db(), _request, new MemoryStorage(_objects), NullLogger<GetBoardItemAttachmentContentQueryHandler>.Instance);

        var act = () => handler.Handle(new GetBoardItemAttachmentContentQuery(ItemId, row.Id), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeletePage_RemovesOnlyAnAttachmentOfThatPage()
    {
        var row = await UploadAsync(PageId);
        var handler = new DeletePageAttachmentCommandHandler(Db(), CurrentUser(), new Clock());

        var wrongPage = () => handler.Handle(new DeletePageAttachmentCommand(OtherPageId, row.Id), default);
        await wrongPage.Should().ThrowAsync<NotFoundException>();
        row.IsDeleted.Should().BeFalse();

        var result = await handler.Handle(new DeletePageAttachmentCommand(PageId, row.Id), default);

        result.Succeeded.Should().BeTrue();
        row.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task BoardItemDelete_DoesNotRemoveAPageAttachment()
    {
        var row = await UploadAsync(PageId);
        var handler = new DeleteAttachmentCommandHandler(Db(), CurrentUser(), new Clock());

        var act = () => handler.Handle(new DeleteAttachmentCommand(row.Id), default);

        await act.Should().ThrowAsync<NotFoundException>();
        row.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task CreateLink_UsesThePageTarget()
    {
        var handler = new CreatePageAttachmentCommandHandler(
            Db(), _request, new Clock(), new InMemoryAttachmentTargetLock(), Options.Create(new AttachmentUploadOptions()));

        var result = await handler.Handle(new CreatePageAttachmentCommand(PageId, "site", "https://example.com/x", null, null), default);

        result.Succeeded.Should().BeTrue();
        _rows.Single().Target.Kind.Value.Should().Be("documents.page");
    }

    [Fact]
    public void BoardItemCreate_StillTargetsTheBoardItem()
    {
        new CreateBoardItemAttachmentCommand(ItemId, "a", "https://example.com", null, null, null).Resource.Kind.Value
            .Should().Be("work-management.board-item");
    }
}
